Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel
Imports System.Management.Automation.Language

Public Class PSScriptParser
    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()

    Private prvScript As String
    Private prvScriptPath As String
    Private prvSynopsis As String
    Private prvDescription As String
    Private prvExamples As New List(Of String)
    Private prvParameterHelp As New Dictionary(Of String, String)
    Private prvPSParam As New List(Of PSCmdParam)
    Private prvIsValid As Boolean = False

    Sub New(scriptPath As String)
        LoadScript(scriptPath)
    End Sub

    Public Sub LoadScript(scriptPath As String)
        dlog.Trace("ScriptParser: LoadScript")
        prvScriptPath = scriptPath
        prvScript = ""
        prvSynopsis = ""
        prvDescription = ""
        prvExamples = New List(Of String)
        prvParameterHelp = New Dictionary(Of String, String)
        prvPSParam = New List(Of PSCmdParam)
        prvIsValid = False

        If Not IO.File.Exists(prvScriptPath) Then
            dlog.Trace("PSScriptParser|File Not Exist: '" & prvScriptPath & "'")
            Return
        End If

        prvScript = GetFileContent(prvScriptPath)

        StartParser()

    End Sub

    Public ReadOnly Property IsValid As Boolean
        Get
            Return prvIsValid
        End Get
    End Property

    Public ReadOnly Property ScriptPath As String
        Get
            Return prvScriptPath
        End Get
    End Property

    Public ReadOnly Property Synopsis As String
        Get
            Return prvSynopsis
        End Get
    End Property

    Public ReadOnly Property Description As String
        Get
            Return prvDescription
        End Get
    End Property

    Public ReadOnly Property Examples As List(Of String)
        Get
            Return prvExamples
        End Get
    End Property

    Public Function GetParameters() As List(Of PSCmdParam)
        Return prvPSParam
    End Function

    Private Sub StartParser()
        If String.IsNullOrWhiteSpace(prvScript) Then Return

        If prvScript.IndexOf("<#") > -1 Then
            ParseCommentBlock()
        End If

        Dim tokens As Token() = Nothing
        Dim errors As ParseError() = Nothing
        Dim ast As ScriptBlockAst = Parser.ParseInput(prvScript, tokens, errors)

        If ast IsNot Nothing AndAlso ast.ParamBlock IsNot Nothing Then
            ParseAstParameters(ast.ParamBlock, tokens)
        End If
    End Sub

    Private Sub ParseCommentBlock()
        dlog.Trace("ScriptParser: Parsing Comment Block")

        Dim startIDX As Integer = prvScript.IndexOf("<#")
        Dim endIDX As Integer = prvScript.IndexOf(vbLf & "#>")
        Dim commentBlock As String

        If (startIDX = -1 Or endIDX = -1) Then
            Return
        End If

        commentBlock = prvScript.Substring(startIDX, endIDX - startIDX)
        Dim cbSet As String() = Regex.Split(commentBlock, "(?=\n\.)")

        For Each cbItem In cbSet
            ParseCommentBlockSection(cbItem.Trim())
        Next

    End Sub

    Private Sub ParseCommentBlockSection(Section As String)
        Section = Section.Trim

        Dim sectionarr() As String = Section.Split(vbLf.ToCharArray, 2)
        If (sectionarr.Count) <> 2 Then Return

        Dim header As String = sectionarr(0).Trim
        Dim comment As String = sectionarr(1).Trim

        If header.StartsWith(".") Then
            header = header.Trim(".")
            If (header.ToUpper = "SYNOPSIS") Then
                dlog.Trace("ScriptParser: CommentBlockSection: Adding SYNOPSIS")
                prvSynopsis = comment
            ElseIf (header.ToUpper = "DESCRIPTION") Then
                dlog.Trace("ScriptParser: CommentBlockSection: Adding DESCRIPTION")
                prvDescription = comment
            ElseIf (header.ToUpper = "EXAMPLE") Then
                dlog.Trace("ScriptParser: CommentBlockSection: Adding EXAMPLE")
                prvExamples.Add(comment)
            ElseIf (header.ToUpper.StartsWith("PARAMETER")) Then
                Dim paramname As String = header.ToUpper.Replace("PARAMETER", "").Trim
                dlog.Trace("ScriptParser: CommentBlockSection: Adding Description for PARAMETER: " & paramname)
                prvParameterHelp.Add(paramname, comment)
            Else
                dlog.Error("ScriptParser: Could not parse commentblock: " & header)
            End If
        End If

    End Sub

    Private Sub ParseAstParameters(paramBlock As ParamBlockAst, tokens As Token())
        dlog.Trace("ScriptParser: ParseAstParameters")

        Dim multilineLines As New HashSet(Of Integer)
        Dim datetimeLines As New HashSet(Of Integer)

        For Each token In tokens
            If token.Kind = TokenKind.Comment Then
                Dim commentText = token.Text.Trim()
                If commentText.Equals("#WEBJEA-MULTILINE", StringComparison.OrdinalIgnoreCase) Then
                    multilineLines.Add(token.Extent.StartLineNumber)
                ElseIf commentText.Equals("#WEBJEA-DATETIME", StringComparison.OrdinalIgnoreCase) Then
                    datetimeLines.Add(token.Extent.StartLineNumber)
                End If
            End If
        Next

        Dim prevEndLine As Integer = paramBlock.Extent.StartLineNumber

        For Each paramAst In paramBlock.Parameters
            Dim psparam As New PSCmdParam

            psparam.Name = paramAst.Name.VariablePath.UserPath
            dlog.Trace("ScriptParser: ParseAstParameters: Processing parameter: " & psparam.Name)

            For Each line In multilineLines
                If line >= prevEndLine AndAlso line <= paramAst.Extent.EndLineNumber Then
                    dlog.Trace("ScriptParser: ParseAstParameters: #WEBJEA-MULTILINE for: " & psparam.Name)
                    psparam.DirectiveMultiline = True
                End If
            Next
            For Each line In datetimeLines
                If line >= prevEndLine AndAlso line <= paramAst.Extent.EndLineNumber Then
                    dlog.Trace("ScriptParser: ParseAstParameters: #WEBJEA-DATETIME for: " & psparam.Name)
                    psparam.DirectiveDateTime = True
                End If
            Next

            For Each attr In paramAst.Attributes
                If TypeOf attr Is TypeConstraintAst Then
                    ProcessTypeConstraint(psparam, DirectCast(attr, TypeConstraintAst))
                ElseIf TypeOf attr Is AttributeAst Then
                    ProcessAttribute(psparam, DirectCast(attr, AttributeAst))
                End If
            Next

            If paramAst.DefaultValue IsNot Nothing Then
                psparam.DefaultValue = ParseDefaultValue(paramAst.DefaultValue.Extent.Text)
            End If

            If prvParameterHelp.ContainsKey(psparam.Name.ToUpper()) Then
                psparam.HelpDetail = prvParameterHelp(psparam.Name.ToUpper())
            End If

            prvPSParam.Add(psparam)
            prevEndLine = paramAst.Extent.EndLineNumber
        Next

        dlog.Trace("ScriptParser: ParseAstParameters: Found " & prvPSParam.Count.ToString() & " parameters")
    End Sub

    Private Sub ProcessTypeConstraint(psparam As PSCmdParam, typeAst As TypeConstraintAst)
        Dim typeName As String = typeAst.TypeName.Name
        dlog.Trace("ScriptParser: ProcessTypeConstraint: " & typeName)

        If typeName.StartsWith("boolean", StringComparison.InvariantCultureIgnoreCase) OrElse
           typeName.StartsWith("datetime", StringComparison.InvariantCultureIgnoreCase) OrElse
           typeName.StartsWith("switch", StringComparison.InvariantCultureIgnoreCase) OrElse
           typeName.StartsWith("int", StringComparison.InvariantCultureIgnoreCase) OrElse
           typeName.StartsWith("uint", StringComparison.InvariantCultureIgnoreCase) OrElse
           typeName.StartsWith("float", StringComparison.InvariantCultureIgnoreCase) OrElse
           typeName.StartsWith("double", StringComparison.InvariantCultureIgnoreCase) OrElse
           typeName.StartsWith("string", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.VarType = typeName
        Else
            dlog.Warn("ScriptParser: Unrecognized type: " & typeName)
        End If
    End Sub

    Private Sub ProcessAttribute(psparam As PSCmdParam, attrAst As AttributeAst)
        Dim attrName = attrAst.TypeName.Name
        dlog.Trace("ScriptParser: ProcessAttribute: " & attrName)

        If String.Equals(attrName, "Parameter", StringComparison.InvariantCultureIgnoreCase) Then
            ProcessParameterAttribute(psparam, attrAst)
        ElseIf attrName.StartsWith("ValidateLength", StringComparison.InvariantCultureIgnoreCase) OrElse
               attrName.StartsWith("ValidateRange", StringComparison.InvariantCultureIgnoreCase) OrElse
               attrName.StartsWith("ValidatePattern", StringComparison.InvariantCultureIgnoreCase) OrElse
               attrName.StartsWith("ValidateCount", StringComparison.InvariantCultureIgnoreCase) OrElse
               attrName.StartsWith("ValidateSet", StringComparison.InvariantCultureIgnoreCase) OrElse
               attrName.StartsWith("ValidateNotNull", StringComparison.InvariantCultureIgnoreCase) OrElse
               attrName.StartsWith("ValidateNotNullOrEmpty", StringComparison.InvariantCultureIgnoreCase) Then
            Dim extentText = attrAst.Extent.Text
            Dim valstring = extentText.Substring(1, extentText.LastIndexOf("]") - 1)
            dlog.Trace("ScriptParser: ProcessAttribute: Adding validation: " & valstring)
            psparam.AddValidation(valstring)
        End If
    End Sub

    Private Sub ProcessParameterAttribute(psparam As PSCmdParam, attrAst As AttributeAst)
        For Each namedArg In attrAst.NamedArguments
            If String.Equals(namedArg.ArgumentName, "Mandatory", StringComparison.InvariantCultureIgnoreCase) Then
                If namedArg.ExpressionOmitted Then
                    dlog.Trace("ScriptParser: ProcessParameterAttribute: Mandatory (expression omitted)")
                    psparam.AddValidation("Mandatory")
                ElseIf TypeOf namedArg.Argument Is VariableExpressionAst Then
                    Dim varExpr = DirectCast(namedArg.Argument, VariableExpressionAst)
                    If Not String.Equals(varExpr.VariablePath.UserPath, "false", StringComparison.InvariantCultureIgnoreCase) Then
                        dlog.Trace("ScriptParser: ProcessParameterAttribute: Mandatory=$true")
                        psparam.AddValidation("Mandatory")
                    End If
                Else
                    psparam.AddValidation("Mandatory")
                End If
            ElseIf String.Equals(namedArg.ArgumentName, "HelpMessage", StringComparison.InvariantCultureIgnoreCase) Then
                If TypeOf namedArg.Argument Is StringConstantExpressionAst Then
                    psparam.HelpMessage = DirectCast(namedArg.Argument, StringConstantExpressionAst).Value
                    dlog.Trace("ScriptParser: ProcessParameterAttribute: HelpMessage: " & psparam.HelpMessage)
                End If
            End If
        Next
    End Sub

    Public Shared Function AdvIndexOf(strInput As String, chars As String, Optional startidx As Integer = -1) As Integer
        'pass one char easily
        Return AdvIndexOf(strInput, New List(Of String)({chars}), startidx)
    End Function

    Public Shared Function AdvIndexOf(strInput As String, chars As List(Of String), Optional startidx As Integer = -1) As Integer
        'this is kind of like indexOf, except we search character by character and if we encounter certain characters ('(','[','{',''','"'),
        '  we recurse the same function until we eventually get the end of file Or find the correct closing character
        'this is because a command like [ValidateScript({$_ -eq "[`"]"})] is valid but would cause a parsing issue if we just indexof the closing character.
        'this may be part of why we'll want to cache the config in the future

        'looks for multiple possible characters, but still support nested
        Dim endIDX As Integer = strInput.Length
        Dim IDX As Integer = startidx

        If strInput = "" Then Return -1

        While IDX < endIDX - 1
            IDX += 1
            Dim IDXchar = strInput.Substring(IDX, 1)
            If IDXchar = "`" Then
                IDX += 1 'skip the next character
            ElseIf IDXchar = "(" Then
                IDX = AdvIndexOf(strInput, ")", IDX)
            ElseIf IDXchar = "[" Then
                IDX = AdvIndexOf(strInput, "]", IDX)
            ElseIf IDXchar = "{" Then
                IDX = AdvIndexOf(strInput, "}", IDX)
            ElseIf IDXchar = """" And Not chars.Contains("""") Then 'if we're looking for a " to return, we don't want to recurse again
                IDX = AdvIndexOf(strInput, """", IDX)
            ElseIf IDXchar = "'" And Not chars.Contains("'") And Not chars.Contains("""") Then 'if we're looking for a ' to return, we don't want to recurse again
                IDX = AdvIndexOf(strInput, "'", IDX)
            Else
                For Each charstr In chars
                    If String.Equals(strInput.Substring(IDX, charstr.Length), charstr, StringComparison.InvariantCultureIgnoreCase) Then
                        Return IDX
                    End If
                Next
            End If

        End While


        'if the character isn't found, then return -1 like indexof
        Return -1

    End Function

    Public Shared Function ReplaceBackTicks(strInput As String) As String
        Return strInput.Replace("`""", """").Replace("`r", vbCr).Replace("`n", vbLf).Replace("`$", "$")
    End Function

    Public Shared Function ParseDefaultValue(strInput As String) As Object
        strInput = strInput.Trim()

        'if number, treat as num
        If IsNumeric(strInput) Then
            Return strInput
            'we dont have to try and parse or convert because it is still just a string in html
        ElseIf strInput.StartsWith("""") Then
            'is a basic string
            Return CleanQuotedString(strInput)
        ElseIf strInput.StartsWith("@") Then
            Dim retList As New List(Of String) 'these have to be strings
            Dim IDX As Integer = strInput.IndexOf("(")
            While IDX < strInput.Length - 1
                Dim endIDX As Integer = AdvIndexOf(strInput, New List(Of String)({",", ")"}), IDX)
                Dim subval As String = strInput.Substring(IDX + 1, endIDX - IDX - 1)
                retList.Add(ParseDefaultValue(subval)) 'this parse properly as string or integer.
                IDX = endIDX
            End While
            Return retList
        ElseIf strInput.StartsWith("$") Then
            If (strInput.ToLower().Contains("true")) Then
                Return True
            ElseIf (strInput.ToLower().Contains("false")) Then
                Return False
            Else
                Return strInput
            End If
        Else
            Return CleanQuotedString(strInput)
        End If
        'if first char is quote, then string
        'if first char is @, then array
        'if first char is $, then t/f?

    End Function

    Public Shared Function CleanQuotedString(strInput As String) As String

        Dim stroutput As String = strInput.Trim
        Dim quotecharidx As Integer = AdvIndexOf(stroutput, New List(Of String)({"""", "'"}))
        If quotecharidx = 0 Then 'wasn't where we expected it to be
            'get the first quote character
            Dim quotechar As String = stroutput.Substring(quotecharidx, 1)

            If quotechar = """" Then stroutput = ReplaceBackTicks(stroutput)

            'remove outer quotes
            stroutput = stroutput.Substring(1, stroutput.Length - 2)
        End If

        Return stroutput
    End Function


End Class
