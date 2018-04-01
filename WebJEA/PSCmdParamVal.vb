Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel

Public Class PSCmdParamVal

    Public Enum ValType
        Mandatory
        Length
        Range
        Pattern
        Count
        SetCol
        Err
    End Enum

    Private prvRule As String
    Public Type As ValType = ValType.Err
    Public UpperLimit As Integer
    Public LowerLimit As Integer
    Public Pattern As String
    Public Options As New List(Of String)

    Sub New(rule As String)

        If rule.Contains("()") Then 'unnecessary
            rule = rule.Replace("()", "")
        End If
        prvRule = rule

        If rule.ToUpper = "VALIDATENOTNULL" Or rule.ToUpper = "VALIDATENOTNULLOREMPTY" Or rule.ToUpper = "ALLOWNULL" Or rule.ToUpper = "ALLOWEMPTYSTRING" Or rule.ToUpper = "ALLOWEMPTYCOLLECTION" Then
            'we can'don't support these at this time.
        ElseIf rule.ToUpper Like "VALIDATESCRIPT(*)" Then
            'can't validate
        ElseIf rule.ToUpper Like "VALIDATELENGTH(*)" Then
            Type = ValType.Length
            ParseRange(prvRule)
        ElseIf rule.ToUpper Like "VALIDATERANGE(*)" Then
            Type = ValType.Range
            ParseRange(prvRule)
        ElseIf rule.ToUpper Like "VALIDATEPATTERN(*)" Then
            Type = ValType.Pattern
            SetPattern(prvRule)
        ElseIf rule.ToUpper Like "VALIDATECOUNT(*)" Then
            Type = ValType.Count
            ParseRange(prvRule)
        ElseIf rule.ToUpper Like "VALIDATESET(*)" Then
            Type = ValType.SetCol
            ParseColSet(prvRule)
        ElseIf rule.ToUpper Like "MANDATORY" Then
            Type = ValType.Mandatory
        Else
            dlog.Error("Don't know how to parse validation rule: " & rule)
        End If

    End Sub

    Private Sub ParseRange(strInput As String)
        Dim pattern As String = "^VALIDATE.+\((?<lowerlimit>\d+),(?<upperlimit>\d+)\)$"
        Dim rgx As New Regex(pattern, RegexOptions.IgnoreCase)
        Dim names() As String = rgx.GetGroupNames()

        Dim mtch As Match = rgx.Match(strInput)
        If mtch.Success Then
            LowerLimit = mtch.Groups("lowerlimit").Value
            UpperLimit = mtch.Groups("upperlimit").Value
        End If

    End Sub

    Private Sub ParseColSet(strInput As String)
        'this command returns the contents of the () in ValidateX(XXX)
        'It then parses by commas and returns a list

        Dim outputset As New List(Of String)

        Dim IDXstart As Integer = strInput.IndexOf("(")
        Dim IDXend As Integer = PSScriptParser.AdvIndexOf(strInput, ")", IDXstart)
        Dim strValues As String = strInput.Substring(IDXstart + 1, IDXend - IDXstart - 1)

        Dim IDXLastComma As Integer = -1
        Dim IDXcomma As Integer = PSScriptParser.AdvIndexOf(strValues, ",")
        While IDXcomma > -1

            outputset.Add(PSScriptParser.CleanQuotedString(strValues.Substring(IDXLastComma + 1, IDXcomma - IDXLastComma - 1)))

            IDXLastComma = IDXcomma
            IDXcomma = PSScriptParser.AdvIndexOf(strValues, ",", IDXLastComma)
        End While
        'add last value
        outputset.Add(PSScriptParser.CleanQuotedString(strValues.Substring(IDXLastComma + 1)))

        Options = outputset
    End Sub



    Private Sub SetPattern(strInput As String)
        Dim ptrn = strInput.Substring(strInput.IndexOf("(")).Trim()
        ptrn = ptrn.Substring(1, ptrn.Length - 2).Trim() 'remove the outer parenthesis
        'should now look like "'xxxxx'" where the interior single quotes could be double quotes
        Pattern = ptrn.Substring(1, ptrn.Length - 2) 'remove the interior quotes
    End Sub

    Public ReadOnly Property IsValid As Boolean
        Get
            'may add other validation
            If (Type = ValType.Err) Then
                Return False
            End If

            Return True

        End Get
    End Property

    Public ReadOnly Property Rule As String
        Get
            Return prvRule
        End Get
    End Property

End Class
