Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel

Public Class PSScriptParser

    Private prvScript As String 'the content of the script
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

        'get content of file into prvscript
        If Not IO.File.Exists(prvScriptPath) Then
            dlog.Trace("PSScriptParser|File Not Exist: '" & prvScriptPath & "'")
            Return
        End If

        prvScript = GetFileContent(prvScriptPath)

        'start parser
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

        'look for <#
        'officially, <# can only be preceeded by whitespace and cr/lf
        'in our case, we don't care.  If the script doesn't work, that will be covered when PS tries to run.
        If prvScript.IndexOf("<#") > -1 Then
            ParseCommentBlock()
        End If


        'same with param(
        'it should only be preceeded by the <##> block, whitespace, and # directives/comments.
        'we also don't care if there's other stuff in the way.  That is the PS engine's problem
        If prvScript.IndexOf("param", StringComparison.InvariantCultureIgnoreCase) > -1 Then
            ParseParameters()
        End If




    End Sub

    Private Sub ParseCommentBlock()
        dlog.Trace("ScriptParser: Parsing Comment Block")

        Dim startIDX As Integer = prvScript.IndexOf("<#")
        Dim endIDX As Integer = prvScript.IndexOf(vbLf & "#>")
        'Dim dividerIDX As Integer
        'Dim dividerIDX2 As Integer
        Dim commentBlock As String

        If (startIDX = -1 Or endIDX = -1) Then
            'comment block isn't valid, can't do anything with it.
            Return
        End If

        commentBlock = prvScript.Substring(startIDX, endIDX - startIDX)
        Dim cbSet As String() = Regex.Split(commentBlock, "(?=\n\.)")

        For Each cbItem In cbSet
            ParseCommentBlockSection(cbItem.Trim())
        Next

        'dividerIDX = prvScript.IndexOf(vbLf & ".", startIDX)
        'While Not (dividerIDX = -1 Or dividerIDX2 >= endIDX)
        '    'find the next entry in the comment block, or if non exists (or > then endidx), set to endidx
        '    dividerIDX2 = prvScript.IndexOf(vbLf & ".", dividerIDX + 1)
        '    If dividerIDX2 = -1 Or dividerIDX2 > endIDX Then
        '        dividerIDX2 = endIDX
        '    End If

        '    Dim section As String = prvScript.Substring(dividerIDX + 1, dividerIDX2 - dividerIDX) 'the +1 and -2 account for the vblf
        '    ParseCommentBlockSection(section)

        '    dividerIDX = dividerIDX2
        'End While

        'copy Parameter help to psparams


    End Sub

    Private Sub ParseCommentBlockSection(Section As String)
        'this should look like:
        '.XXXXXXX
        'string
        'more string
        ' <expected, extra line break, but not necessary as it will be trimmed automatically>

        Section = Section.Trim 'trim leading and trailing spaces, cr,lf, etc

        Dim sectionarr() As String = Section.Split(vbLf.ToCharArray, 2)
        Dim header As String = sectionarr(0).Trim
        Dim comment As String = sectionarr(1).Trim

        If header.StartsWith(".") Then 'want to verify it has the ., then we remove it
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

    Private Sub ParseParameters()
        dlog.Trace("ScriptParser: ParseParameters")
        Dim IDX As Integer = 0
        Dim psparam As New PSCmdParam

        Dim endIDX As Integer = prvScript.Length

        'check for <#
        Dim commentIDX As Integer = prvScript.IndexOf("<#")
        If commentIDX > -1 Then
            'found a comment block that MIGHT have 'param' in it

            IDX = prvScript.IndexOf("#>", commentIDX)
            If IDX = -1 Then
                dlog.Error("ScriptParser: Did not find end of comment block")
            End If
            IDX += 2 'skip the #>
            dlog.Trace("ScriptParsers: ParseParameters: Found Comment Block " & commentIDX.ToString & " to " & IDX.ToString)
        End If

        'get the idx of param
        Dim paramMatch As Match = Regex.Match(prvScript, "(^|\n)param\s*\(", RegexOptions.IgnoreCase)
        If paramMatch.Success Then
            dlog.Trace("ScriptParser: ParseParameters: Found param() block")
            'found a properly formatted param( that isn't some function or something undesired.  must be new line, param, whitespace (incl crlf), then (
            IDX = paramMatch.Index
        Else
            dlog.Trace("ScriptParser: ParseParameters: Did NOT find param() block")
            'didn't find a parameter block
            Return
        End If

        'then get the idx of (
        IDX = prvScript.IndexOf("(", IDX)
        endIDX = AdvIndexOf(prvScript, ")", IDX)
        Dim FoundCloseTag As Boolean = False

        If endIDX = -1 Then endIDX = prvScript.Length 'probably means the param block doesnt have a proper close tag)
        dlog.Trace("ScriptParser: ParseParameters: IDX Start: " & IDX & " IDX End: " & endIDX)
        'we've entered the param block

        While Not FoundCloseTag And IDX < endIDX
            'we should not see a close param, until the end of the param block.
            '  close params we do see should handled before it gets to the while loop

            'the -1 on idx=closeidx is so we catch every character, other wise we'll skip a char each time.

            IDX += 1
            Dim idxchar As String = prvScript.Substring(IDX, 1) 'just for debugging
            Select Case prvScript.Substring(IDX, 1)
                Case "["
                    Dim closeIDX As Integer = AdvIndexOf(prvScript, "]", IDX)
                    Dim valstring As String = prvScript.Substring(IDX, closeIDX - IDX + 1)
                    dlog.Trace("ScriptParser: ParseParameters: Processing []: " & valstring)
                    ParseParameterString(psparam, valstring)
                    IDX = closeIDX
                Case "$"
                    Dim closeIDX As Integer = AdvIndexOf(prvScript, New List(Of String)({",", ")", "=", vbLf, " ", vbTab}), IDX)
                    Dim valstring As String = prvScript.Substring(IDX + 1, closeIDX - IDX - 1).Trim
                    dlog.Trace("ScriptParser: ParseParameters: Processing $: " & valstring)
                    psparam.Name = valstring
                    'check if there's a helpdetail, if so, add it now
                    If prvParameterHelp.ContainsKey(valstring.ToUpper) Then
                        psparam.HelpDetail = prvParameterHelp(valstring.ToUpper)
                    End If
                    IDX = closeIDX - 1 'subtract one because we didn't use the matched character and we want to evaluate it on the next step
                Case "=" 'default value
                    Dim closeIDX As Integer = AdvIndexOf(prvScript, New List(Of String)({",", ")", "#"}), IDX)
                    Dim valstring As String = prvScript.Substring(IDX + 1, closeIDX - IDX - 1)
                    dlog.Trace("ScriptParser: ParseParameters: Processing =: " & valstring)
                    psparam.DefaultValue = ParseDefaultValue(valstring)
                    IDX = closeIDX - 1 'subtract one because we didn't use the matched character and we want to evaluate it on the next step
                Case "#"
                    'just get to end of line
                    Dim closeIDX As Integer = AdvIndexOf(prvScript, vbLf, IDX)
                    If prvScript.Substring(IDX, closeIDX - IDX).ToUpper() = ("#WEBJEA-MULTILINE" & vbCr) Then
                        dlog.Trace("ScriptParser: ParseParameters: Processing #WEBJEA directive: Multiline")
                        psparam.DirectiveMultiline = True
                    ElseIf prvScript.Substring(IDX, closeIDX - IDX).ToUpper() = ("#WEBJEA-DATETIME" & vbCr) Then
                        dlog.Trace("ScriptParser: ParseParameters: Processing #WEBJEA directive: DateTime")
                        psparam.DirectiveDateTime = True
                    Else
                        dlog.Trace("ScriptParser: ParseParameters: Processing #: Skipping to IDX: " & closeIDX)
                    End If
                    IDX = closeIDX
                Case "<"
                    'this is for <# #> but can just shortcut to <> to match this code, but may be well supported to use <# #>
                    Dim closeIDX As Integer = AdvIndexOf(prvScript, ">", IDX)
                    dlog.Trace("ScriptParser: ParseParameters: Processing <##>: Skipping to IDX: " & IDX)
                    IDX = closeIDX
                Case ","
                    '    complete the param and add to code
                    If Not (psparam Is Nothing) Then 'commit the param and then set it to nothing so the earlier code works.
                        dlog.Trace("ScriptParser: ParseParameters: Adding Parameter to Set: " & psparam.Name)
                        prvPSParam.Add(psparam.Clone)
                        psparam = New PSCmdParam
                    End If
                Case ")"
                    'complete the param and add to code
                    If Not (psparam Is Nothing) And psparam.Name <> "" Then 'commit the param and then set it to nothing so the earlier code works.
                        dlog.Trace("ScriptParser: ParseParameters: Adding Parameter to Set: " & psparam.Name)
                        prvPSParam.Add(psparam.Clone)
                        psparam = New PSCmdParam
                    End If
                    FoundCloseTag = True
                    'this should be the end of the param block, should be processed as the end of the while loop in a moment
                Case Else
                    'Nothing to do, hopefully, should just be whitespace characters
            End Select
        End While

        dlog.Trace("ScriptParser: ParseParameters: Completed Parsing at IDX: " & IDX)

    End Sub

    Private Sub ParseParameterString(psparam As PSCmdParam, valstring As String)

        'trim the outer []
        valstring = valstring.Substring(1, valstring.LastIndexOf("]") - 1)
        '**************************
        'Parameter() parsing
        '**************************
        If valstring.StartsWith("Parameter", StringComparison.InvariantCultureIgnoreCase) Then

            'look for mandatory=true/false
            Dim idxMandatory As Integer = valstring.IndexOf("Mandatory", StringComparison.InvariantCultureIgnoreCase)
            If idxMandatory > -1 Then
                Dim idxSepMand As Integer = AdvIndexOf(valstring, New List(Of String)({",", ")"}), idxMandatory)

                Dim strMandatory As String = valstring.Substring(idxMandatory, idxSepMand - idxMandatory)
                If strMandatory.IndexOf("false", StringComparison.InvariantCultureIgnoreCase) > -1 Then
                    'not mandatory
                Else
                    dlog.Trace("ScriptParser: ParseParameters: Mandatory=$true")
                    psparam.AddValidation("Mandatory")
                End If

            End If

            'look for helpmessage
            Dim idxHelp As Integer = valstring.IndexOf("HelpMessage", StringComparison.InvariantCultureIgnoreCase)
            If idxHelp > -1 Then
                Dim idxSepHelp As Integer = AdvIndexOf(valstring, New List(Of String)({",", ")"}), idxHelp)

                Dim strHelpMsg As String = valstring.Substring(idxHelp, idxSepHelp - idxHelp)
                Dim idxHelpMsgStart As Integer = AdvIndexOf(strHelpMsg, New List(Of String)({"'", """"}))
                Dim idxHelpMsgStartChar As String = strHelpMsg.Substring(idxHelpMsgStart, 1)
                Dim idxHelpMsgEnd As Integer = AdvIndexOf(strHelpMsg, idxHelpMsgStartChar, idxHelpMsgStart)

                Dim strHelpMsgContent As String = strHelpMsg.Substring(idxHelpMsgStart + 1, idxHelpMsgEnd - idxHelpMsgStart - 1)

                dlog.Trace("ScriptParser: ParseParameters: Help: " & strHelpMsgContent)
                psparam.HelpMessage = strHelpMsgContent

            End If

            '**************************
            'Permitted Validation Types
            '**************************
        ElseIf valstring.StartsWith("ValidateLength", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.AddValidation(valstring)
        ElseIf valstring.StartsWith("ValidateRange", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.AddValidation(valstring)
        ElseIf valstring.StartsWith("ValidatePattern", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.AddValidation(valstring)
        ElseIf valstring.StartsWith("ValidateCount", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.AddValidation(valstring)
        ElseIf valstring.StartsWith("ValidateSet", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.AddValidation(valstring)

            '**************************
            'Variable Types
            '**************************
        ElseIf valstring.StartsWith("boolean", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.VarType = valstring
        ElseIf valstring.StartsWith("datetime", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.VarType = valstring
        ElseIf valstring.StartsWith("switch", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.VarType = valstring
        ElseIf valstring.StartsWith("int", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.VarType = valstring
        ElseIf valstring.StartsWith("uint", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.VarType = valstring
        ElseIf valstring.StartsWith("float", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.VarType = valstring
        ElseIf valstring.StartsWith("double", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.VarType = valstring
        ElseIf valstring.StartsWith("string", StringComparison.InvariantCultureIgnoreCase) Then
            psparam.VarType = valstring

            '**************************
            'Ignore These
            '**************************
        ElseIf valstring.StartsWith("Alias(", StringComparison.InvariantCultureIgnoreCase) Then
            'ignore
        ElseIf valstring.StartsWith("ValidateScript", StringComparison.InvariantCultureIgnoreCase) Then
            'ignore
        ElseIf valstring.StartsWith("ValidateNotNullOrEmpty", StringComparison.InvariantCultureIgnoreCase) Then
            'ignore
        ElseIf valstring.StartsWith("ValidateNotNull", StringComparison.InvariantCultureIgnoreCase) Then
            'ignore
        ElseIf valstring.StartsWith("AllowNull", StringComparison.InvariantCultureIgnoreCase) Then
            'ignore
        ElseIf valstring.StartsWith("AllowEmptyString", StringComparison.InvariantCultureIgnoreCase) Then
            'ignore
        ElseIf valstring.StartsWith("AllowEmptyCollection", StringComparison.InvariantCultureIgnoreCase) Then
            'ignore
        ElseIf valstring.StartsWith("SupportsWildcards", StringComparison.InvariantCultureIgnoreCase) Then
            'ignore
        Else
            dlog.Warn("ScriptParser: Did not expect value: " & valstring)
        End If


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
