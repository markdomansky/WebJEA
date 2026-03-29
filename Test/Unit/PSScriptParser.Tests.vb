Imports System
Imports Xunit
Imports System.IO
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class PSScriptParserTests

        Private ReadOnly TestScriptsPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\..\..\TestScripts\")

        Private Function GetScriptPath(scriptName As String) As String
            Return Path.GetFullPath(Path.Combine(TestScriptsPath, scriptName))
        End Function

        Private Function FindParam(parameters As List(Of PSCmdParam), name As String) As PSCmdParam
            Return parameters.FirstOrDefault(Function(p) p.Name = name)
        End Function

#Region "Scripts with no content or missing files"

        <Fact>
        Public Sub NonExistentFile_ShouldNotBeValid()
            Dim parser As New PSScriptParser(GetScriptPath("NonExistentScript.ps1"))

            Assert.False(parser.IsValid)
        End Sub

        <Fact>
        Public Sub EmptyScript_ShouldNotBeValidAndHaveNoContent()
            Dim parser As New PSScriptParser(GetScriptPath("EmptyScript.ps1"))

            Assert.False(parser.IsValid)
            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
            Assert.Empty(parser.GetParameters())
        End Sub

#End Region

#Region "NoComments.ps1 - params only, no comment block"

        <Fact>
        Public Sub NoComments_ShouldParseParametersWithNoCommentBlock()
            Dim parser As New PSScriptParser(GetScriptPath("NoComments.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)

            Assert.Equal(2, parameters.Count)

            Dim param1 = FindParam(parameters, "Param1")
            Assert.NotNull(param1)
            Assert.Equal("string", param1.VarType)
            Assert.False(param1.IsMandatory)
            Assert.Null(param1.DefaultValue)

            Dim param2 = FindParam(parameters, "Param2")
            Assert.NotNull(param2)
            Assert.Equal("42", CStr(param2.DefaultValue))
        End Sub

#End Region

#Region "t1-getprocess.ps1 - simple params without types or comment block"

        <Fact>
        Public Sub T1GetProcess_ShouldParseUntypedParamsWithNoCommentBlock()
            Dim parser As New PSScriptParser(GetScriptPath("t1-getprocess.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)

            Assert.Equal(3, parameters.Count)
            Assert.Equal("name", parameters(0).Name)
            Assert.Equal("webjeahostname", parameters(1).Name)
            Assert.Equal("webjeausername", parameters(2).Name)

            For Each p In parameters
                Assert.Empty(p.VarType)
                Assert.False(p.IsMandatory)
            Next
        End Sub

#End Region

#Region "t2-noparam.ps1 - empty param block"

        <Fact>
        Public Sub T2NoParam_ShouldReturnNoParameters()
            Dim parser As New PSScriptParser(GetScriptPath("t2-noparam.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
            Assert.Empty(parameters)
            Assert.Equal(GetScriptPath("t2-noparam.ps1"), parser.ScriptPath)
        End Sub

#End Region

#Region "t4-Listbox.ps1 - comment block, ValidateSet, ValidateCount, Mandatory"

        <Fact>
        Public Sub T4Listbox_ShouldParseCommentBlockAndConstrainedParameters()
            Dim parser As New PSScriptParser(GetScriptPath("t4-Listbox.ps1"))
            Dim parameters = parser.GetParameters()

            ' Comment block
            Assert.Equal("Describe the function here", parser.Synopsis)
            Assert.Equal("Describe the function in more detail", parser.Description)
            Assert.Equal(2, parser.Examples.Count)
            Assert.Equal("Give an example of how to use it", parser.Examples(0))
            Assert.Equal("Give another example of how to use it", parser.Examples(1))

            ' Parameters
            Assert.Equal(2, parameters.Count)

            Dim input2 = FindParam(parameters, "Input2")
            Assert.NotNull(input2)
            Assert.Equal("string[]", input2.VarType)
            Assert.False(input2.IsMandatory)
            Assert.True(input2.IsMultiValued)
            Assert.True(input2.IsSelect)

            Dim input1 = FindParam(parameters, "Input1")
            Assert.NotNull(input1)
            Assert.Equal("string", input1.VarType)
            Assert.True(input1.IsMandatory)
            Assert.True(input1.IsSelect)
            Dim allowedVals = input1.AllowedValues
            Assert.Contains("Input", allowedVals)
            Assert.Contains("Output", allowedVals)
            Assert.Contains("Both", allowedVals)
        End Sub

#End Region

#Region "t5-Date.ps1 - datetime params, WEBJEA-DateTime directive"

        <Fact>
        Public Sub T5Date_ShouldParseDateTimeParamsAndDirective()
            Dim parser As New PSScriptParser(GetScriptPath("t5-Date.ps1"))
            Dim parameters = parser.GetParameters()

            ' Comment block
            Assert.Equal("Describe the function here", parser.Synopsis)
            Assert.Contains("BLAH", parser.Description)
            Assert.Equal(2, parser.Examples.Count)

            ' Parameters
            Assert.Equal(3, parameters.Count)

            Dim input01 = FindParam(parameters, "Input01DT")
            Assert.NotNull(input01)
            Assert.Equal("datetime", input01.VarType)
            Assert.False(input01.DirectiveDateTime)

            Dim input02 = FindParam(parameters, "Input02DT")
            Assert.NotNull(input02)
            Assert.Equal("datetime", input02.VarType)
            Assert.True(input02.DirectiveDateTime)

            Dim input03 = FindParam(parameters, "Input03DT")
            Assert.NotNull(input03)
            Assert.True(input03.IsMandatory)
            Assert.Empty(input03.VarType)
        End Sub

#End Region

#Region "t6-Int.ps1 - integer and numeric types"

        <Fact>
        Public Sub T6Int_ShouldParseIntegerAndNumericTypes()
            Dim parser As New PSScriptParser(GetScriptPath("t6-Int.ps1"))
            Dim parameters = parser.GetParameters()

            ' Same comment block as t5
            Assert.Equal("Describe the function here", parser.Synopsis)
            Assert.Contains("BLAH", parser.Description)
            Assert.Equal(2, parser.Examples.Count)

            Dim input01 = FindParam(parameters, "Input01")
            Assert.NotNull(input01)
            Assert.True(input01.VarType.StartsWith("int", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(PSCmdParam.ParameterType.PSInt, input01.ParamType)

            Dim input02 = FindParam(parameters, "Input02")
            Assert.NotNull(input02)
            Assert.Contains("[]", input02.VarType)
            Assert.True(input02.IsMultiValued)

            Dim input03 = FindParam(parameters, "Input03")
            Assert.NotNull(input03)
            Assert.True(input03.VarType.StartsWith("uint", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(PSCmdParam.ParameterType.PSInt, input03.ParamType)
        End Sub

#End Region

#Region "t7-Parser.ps1 - comprehensive param types, directives, defaults, validations"

        <Fact>
        Public Sub T7Parser_ShouldParseAllParameterTypesAndAttributes()
            Dim parser As New PSScriptParser(GetScriptPath("t7-Parser.ps1"))
            Dim parameters = parser.GetParameters()

            ' Comment block
            Assert.Equal("Describe the function here", parser.Synopsis)
            Assert.Contains("BLAH", parser.Description)
            Assert.Equal(2, parser.Examples.Count)
            Assert.Equal("Give an example of how to use it", parser.Examples(0))
            Assert.Equal("Give another example of how to use it", parser.Examples(1))

            ' Total parameter count (includes WebJEAUsername/WebJEAuserHostname)
            Assert.True(parameters.Count >= 14)

            ' Input01Mandatory - string, mandatory, HelpMessage, HelpDetail from comment block
            Dim p01 = FindParam(parameters, "Input01Mandatory")
            Assert.NotNull(p01)
            Assert.Equal("string", p01.VarType)
            Assert.True(p01.IsMandatory)
            Assert.Equal("What computer name would you like to target?", p01.HelpMessage)
            Assert.Contains("computer", p01.HelpDetail.ToLower())

            ' Input02MandatoryMinLen - mandatory, ValidateLength, default value "ABC"
            Dim p02 = FindParam(parameters, "Input02MandatoryMinLen")
            Assert.NotNull(p02)
            Assert.Equal("string", p02.VarType)
            Assert.True(p02.IsMandatory)
            Assert.Equal("ABC", CStr(p02.DefaultValue))
            Assert.True(p02.Validation.Any(Function(v) v.StartsWith("ValidateLength", StringComparison.OrdinalIgnoreCase)))

            ' Input03Str - WEBJEA-Multiline directive, not mandatory
            Dim p03 = FindParam(parameters, "Input03Str")
            Assert.NotNull(p03)
            Assert.True(p03.DirectiveMultiline)
            Assert.False(p03.IsMandatory)
            Assert.Contains("file", p03.HelpDetail.ToLower())

            ' Input04Range - int32, ValidateRange
            Dim p04 = FindParam(parameters, "Input04Range")
            Assert.NotNull(p04)
            Assert.Equal("int32", p04.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSInt, p04.ParamType)
            Assert.Equal("What value would you like to enter?", p04.HelpMessage)
            Assert.True(p04.Validation.Any(Function(v) v.StartsWith("ValidateRange", StringComparison.OrdinalIgnoreCase)))

            ' Input05Script - mandatory, default "ABCD", ValidateScript ignored
            Dim p05 = FindParam(parameters, "Input05Script")
            Assert.NotNull(p05)
            Assert.True(p05.IsMandatory)
            Assert.Equal("ABCD", CStr(p05.DefaultValue))

            ' Input06DT - datetime
            Dim p06 = FindParam(parameters, "Input06DT")
            Assert.NotNull(p06)
            Assert.Equal("datetime", p06.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSDate, p06.ParamType)

            ' Input07Regex - ValidatePattern
            Dim p07 = FindParam(parameters, "Input07Regex")
            Assert.NotNull(p07)
            Assert.Equal("string", p07.VarType)
            Assert.True(p07.Validation.Any(Function(v) v.StartsWith("ValidatePattern", StringComparison.OrdinalIgnoreCase)))

            ' Input08StrSetUpto5 - string[], ValidateCount
            Dim p08 = FindParam(parameters, "Input08StrSetUpto5")
            Assert.NotNull(p08)
            Assert.Equal("string[]", p08.VarType)
            Assert.True(p08.IsMultiValued)
            Assert.True(p08.Validation.Any(Function(v) v.StartsWith("ValidateCount", StringComparison.OrdinalIgnoreCase)))

            ' Input09ConstrainedSet - ValidateSet with single select
            Dim p09 = FindParam(parameters, "Input09ConstrainedSet")
            Assert.NotNull(p09)
            Assert.True(p09.IsSelect)
            Assert.False(p09.IsMultiValued)

            ' Input14ConstrainedSet - ValidateSet with multi-select (string[])
            Dim p14 = FindParam(parameters, "Input14ConstrainedSet")
            Assert.NotNull(p14)
            Assert.True(p14.IsSelect)
            Assert.True(p14.IsMultiValued)

            ' Input10NoVarType - no type specified
            Dim p10 = FindParam(parameters, "Input10NoVarType")
            Assert.NotNull(p10)
            Assert.Empty(p10.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p10.ParamType)

            ' Input11Switch - switch type
            Dim p11 = FindParam(parameters, "Input11Switch")
            Assert.NotNull(p11)
            Assert.Equal("switch", p11.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSBoolean, p11.ParamType)
            Assert.True(p11.IsMandatory)
            Assert.Contains("terms", p11.HelpDetail.ToLower())

            ' Input12Bool - boolean type
            Dim p12 = FindParam(parameters, "Input12Bool")
            Assert.NotNull(p12)
            Assert.Equal("boolean", p12.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSBoolean, p12.ParamType)

            ' Input13NotNullEmpty - WEBJEA-Multiline, default 'x'
            Dim p13 = FindParam(parameters, "Input13NotNullEmpty")
            Assert.NotNull(p13)
            Assert.True(p13.DirectiveMultiline)
            Assert.Equal("x", CStr(p13.DefaultValue))
        End Sub

#End Region

#Region "t8-Text.ps1 - similar to t7 with spacing variations"

        <Fact>
        Public Sub T8Text_ShouldParseParamsWithAlternateSpacing()
            Dim parser As New PSScriptParser(GetScriptPath("t8-Text.ps1"))
            Dim parameters = parser.GetParameters()

            ' Comment block
            Assert.Equal("Describe the function here", parser.Synopsis)
            Assert.Contains("BLAH", parser.Description)
            Assert.Equal(2, parser.Examples.Count)

            ' Spot-check key parameters exist and are correctly parsed
            Dim p01 = FindParam(parameters, "Input01Mandatory")
            Assert.NotNull(p01)
            Assert.True(p01.IsMandatory)
            Assert.Equal("string", p01.VarType)

            Dim p02 = FindParam(parameters, "Input02MandatoryMinLen")
            Assert.NotNull(p02)
            Assert.True(p02.IsMandatory)
            Assert.Equal("ABC", CStr(p02.DefaultValue))

            Dim p03 = FindParam(parameters, "Input03Str")
            Assert.NotNull(p03)
            Assert.True(p03.DirectiveMultiline)

            Dim p11 = FindParam(parameters, "Input11Switch")
            Assert.NotNull(p11)
            Assert.Equal("switch", p11.VarType)
            Assert.True(p11.IsMandatory)

            Dim p12 = FindParam(parameters, "Input12Bool")
            Assert.NotNull(p12)
            Assert.Equal("boolean", p12.VarType)
            Assert.Equal(True, p12.DefaultValue)

            Dim p10 = FindParam(parameters, "Input10NoVarType")
            Assert.NotNull(p10)
            Assert.Empty(p10.VarType)

            Dim p09b = FindParam(parameters, "Input09BConstrainedSet")
            Assert.NotNull(p09b)
            Assert.True(p09b.IsSelect)
            Assert.True(p09b.IsMultiValued)
        End Sub

#End Region

#Region "t9-Multi.ps1 - multi-select, array defaults, constrained sets"

        <Fact>
        Public Sub T9Multi_ShouldParseArrayDefaultsAndConstrainedSets()
            Dim parser As New PSScriptParser(GetScriptPath("t9-Multi.ps1"))
            Dim parameters = parser.GetParameters()

            ' Comment block
            Assert.Equal("Describe the function here", parser.Synopsis)
            Assert.Contains("BLAH", parser.Description)
            Assert.Equal(2, parser.Examples.Count)

            ' Input03Str - multiline directive with HelpMessage
            Dim p03 = FindParam(parameters, "Input03Str")
            Assert.NotNull(p03)
            Assert.True(p03.DirectiveMultiline)
            Assert.Equal("Help Message on MultiLine Directive", p03.HelpMessage)

            ' Input07Regex - ValidatePattern
            Dim p07 = FindParam(parameters, "Input07Regex")
            Assert.NotNull(p07)
            Assert.True(p07.Validation.Any(Function(v) v.StartsWith("ValidatePattern", StringComparison.OrdinalIgnoreCase)))

            ' Input08StrSetUpto5 - array default @('A',"B")
            Dim p08 = FindParam(parameters, "Input08StrSetUpto5")
            Assert.NotNull(p08)
            Assert.IsType(Of List(Of String))(p08.DefaultValue)
            Dim defaultList = DirectCast(p08.DefaultValue, List(Of String))
            Assert.Contains("A", defaultList)
            Assert.Contains("B", defaultList)

            ' Input09ConstrainedSet - single select
            Dim p09 = FindParam(parameters, "Input09ConstrainedSet")
            Assert.NotNull(p09)
            Assert.True(p09.IsSelect)
            Assert.False(p09.IsMultiValued)

            ' Input09BConstrainedSet - multi-select with array default
            Dim p09b = FindParam(parameters, "Input09BConstrainedSet")
            Assert.NotNull(p09b)
            Assert.True(p09b.IsSelect)
            Assert.True(p09b.IsMultiValued)
            Assert.IsType(Of List(Of String))(p09b.DefaultValue)
            Dim defaultList09b = DirectCast(p09b.DefaultValue, List(Of String))
            Assert.Contains("a", defaultList09b)
            Assert.Contains("d", defaultList09b)

            ' Input09CConstrainedSet - mandatory multi-select
            Dim p09c = FindParam(parameters, "Input09CConstrainedSet")
            Assert.NotNull(p09c)
            Assert.True(p09c.IsMandatory)
            Assert.True(p09c.IsSelect)
            Assert.True(p09c.IsMultiValued)
        End Sub

#End Region

#Region "t10o.ps1 - comment block with WebJEA-only params"

        <Fact>
        Public Sub T10o_ShouldParseCommentBlockAndWebJEAParams()
            Dim parser As New PSScriptParser(GetScriptPath("t10o.ps1"))
            Dim parameters = parser.GetParameters()

            ' Comment block with nested brackets in first line should still parse
            Assert.Equal("Describe the function here", parser.Synopsis)
            Assert.Contains("BLAH", parser.Description)
            Assert.Equal(2, parser.Examples.Count)

            ' Only has WebJEAUsername and WebJEAHostname params
            Assert.Equal(2, parameters.Count)
            Assert.NotNull(FindParam(parameters, "WebJEAUsername"))
            Assert.NotNull(FindParam(parameters, "WebJEAHostname"))
        End Sub

#End Region

#Region "t11o.ps1 / t12o.ps1 - no param block, just inline params"

        <Fact>
        Public Sub T11o_ShouldParseInlineParamsNoCommentBlock()
            Dim parser As New PSScriptParser(GetScriptPath("t11o.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)

            Assert.Equal(5, parameters.Count)
            Assert.NotNull(FindParam(parameters, "WebJEAUsername"))
            Assert.NotNull(FindParam(parameters, "WebJEAHostname"))
            Assert.NotNull(FindParam(parameters, "p1"))
            Assert.NotNull(FindParam(parameters, "p2"))
            Assert.NotNull(FindParam(parameters, "p3"))
        End Sub

        <Fact>
        Public Sub T12o_ShouldParseInlineParamsNoCommentBlock()
            Dim parser As New PSScriptParser(GetScriptPath("t12o.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)

            Assert.Equal(5, parameters.Count)
            Assert.NotNull(FindParam(parameters, "WebJEAUsername"))
            Assert.NotNull(FindParam(parameters, "WebJEAHostname"))
            Assert.NotNull(FindParam(parameters, "p1"))
            Assert.NotNull(FindParam(parameters, "p2"))
            Assert.NotNull(FindParam(parameters, "p3"))
        End Sub

#End Region

#Region "t13.ps1 - unrecognized type (pscredential)"

        <Fact>
        Public Sub T13_ShouldParseUnrecognizedTypeAsEmptyVarType()
            Dim parser As New PSScriptParser(GetScriptPath("t13.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)

            Assert.Single(parameters)
            Assert.Equal("TestCred", parameters(0).Name)
            Assert.Empty(parameters(0).VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, parameters(0).ParamType)
        End Sub

#End Region

#Region "t14.ps1 - unrecognized type (securestring)"

        <Fact>
        Public Sub T14_ShouldParseSecureStringAsEmptyVarType()
            Dim parser As New PSScriptParser(GetScriptPath("t14.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)

            Assert.Single(parameters)
            Assert.Equal("secstr", parameters(0).Name)
            Assert.Empty(parameters(0).VarType)
        End Sub

#End Region

#Region "t15.ps1 - pscredential and securestring with comment block"

        <Fact>
        Public Sub T15_ShouldParseMixedUnrecognizedTypesWithCommentBlock()
            Dim parser As New PSScriptParser(GetScriptPath("t15.ps1"))
            Dim parameters = parser.GetParameters()

            ' Comment block
            Assert.Equal("Describe the function here", parser.Synopsis)
            Assert.Contains("BLAH", parser.Description)
            Assert.Equal(2, parser.Examples.Count)

            ' Both params have unrecognized types
            Assert.Equal(2, parameters.Count)

            Dim cred = FindParam(parameters, "cred")
            Assert.NotNull(cred)
            Assert.Empty(cred.VarType)
            Assert.True(cred.IsMandatory)
            Assert.Equal("Enter your credential", cred.HelpMessage)

            Dim secstr = FindParam(parameters, "secstr")
            Assert.NotNull(secstr)
            Assert.Empty(secstr.VarType)
            Assert.True(secstr.IsMandatory)
            Assert.Equal("Verify your PW", secstr.HelpMessage)
        End Sub

#End Region

#Region "e0.ps1 - incomplete comment block (no closing #>)"

        <Fact>
        Public Sub E0_ShouldNotParseIncompleteCommentBlock()
            Dim parser As New PSScriptParser(GetScriptPath("e0.ps1"))

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
        End Sub

#End Region

#Region "t0-onload.ps1 - no param block, no comment block"

        <Fact>
        Public Sub T0OnLoad_ShouldHaveNoCommentBlockOrParameters()
            Dim parser As New PSScriptParser(GetScriptPath("t0-onload.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
            Assert.Empty(parameters)
        End Sub

#End Region

#Region "ValidScript.ps1 - simple comment block and typed params"

        <Fact>
        Public Sub ValidScript_ShouldParseSimpleCommentBlockAndParams()
            Dim parser As New PSScriptParser(GetScriptPath("ValidScript.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Equal("This is a valid script.", parser.Synopsis)
            Assert.Equal("This script is used for testing.", parser.Description)
            Assert.Single(parser.Examples)
            Assert.Equal("Example usage of the script.", parser.Examples(0))

            Assert.Equal(2, parameters.Count)

            Dim param1 = FindParam(parameters, "Param1")
            Assert.NotNull(param1)
            Assert.Equal("string", param1.VarType)

            Dim param2 = FindParam(parameters, "Param2")
            Assert.NotNull(param2)
            Assert.Equal("42", CStr(param2.DefaultValue))
        End Sub

#End Region

#Region "InvalidScript.ps1 - incomplete comment block (no closing #>)"

        <Fact>
        Public Sub InvalidScript_ShouldNotParseIncompleteCommentBlock()
            Dim parser As New PSScriptParser(GetScriptPath("InvalidScript.ps1"))

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
        End Sub

#End Region

#Region "ComplexScript.ps1 - comment block with ValidatePattern"

        <Fact>
        Public Sub ComplexScript_ShouldParseCommentBlockAndValidatePattern()
            Dim parser As New PSScriptParser(GetScriptPath("ComplexScript.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Equal("Complex script.", parser.Synopsis)
            Assert.Equal("This script has nested and escaped characters.", parser.Description)
            Assert.Single(parser.Examples)
            Assert.Equal("Example usage.", parser.Examples(0))

            Assert.Equal(2, parameters.Count)

            Dim param1 = FindParam(parameters, "Param1")
            Assert.NotNull(param1)
            Assert.Equal("string", param1.VarType)
            Assert.True(param1.Validation.Any(Function(v) v.StartsWith("ValidatePattern", StringComparison.OrdinalIgnoreCase)))

            Dim param2 = FindParam(parameters, "Param2")
            Assert.NotNull(param2)
            Assert.Equal("42", CStr(param2.DefaultValue))
        End Sub

#End Region

#Region "validate.ps1 - comprehensive template with all param types"

        <Fact>
        Public Sub Validate_ShouldParseFullTemplateWithAllParamTypes()
            Dim parser As New PSScriptParser(GetScriptPath("validate.ps1"))
            Dim parameters = parser.GetParameters()

            ' Comment block
            Assert.Equal("Short 1 line description of what this script does.", parser.Synopsis)
            Assert.Contains("Detailed description", parser.Description)
            Assert.Single(parser.Examples)
            Assert.Contains("ScriptTemplate.ps1", parser.Examples(0))

            ' String params
            Dim p01 = FindParam(parameters, "Input01Mandatory")
            Assert.NotNull(p01)
            Assert.True(p01.IsMandatory)
            Assert.Equal("string", p01.VarType)
            Assert.Contains("computer", p01.HelpDetail.ToLower())

            Dim p02 = FindParam(parameters, "Input02MandatoryMinLen")
            Assert.NotNull(p02)
            Assert.True(p02.IsMandatory)
            Assert.Equal("ABC", CStr(p02.DefaultValue))

            Dim p03 = FindParam(parameters, "Input03Str")
            Assert.NotNull(p03)
            Assert.True(p03.DirectiveMultiline)

            Dim p13 = FindParam(parameters, "Input13NotNullEmpty")
            Assert.NotNull(p13)
            Assert.True(p13.DirectiveMultiline)
            Assert.Equal("x", CStr(p13.DefaultValue))

            ' Number params
            Dim nInput01 = FindParam(parameters, "NInput01")
            Assert.NotNull(nInput01)
            Assert.True(nInput01.VarType.StartsWith("int", StringComparison.OrdinalIgnoreCase))

            Dim nInput02 = FindParam(parameters, "NInput02")
            Assert.NotNull(nInput02)
            Assert.True(nInput02.IsMultiValued)

            Dim nInput3 = FindParam(parameters, "NInput3")
            Assert.NotNull(nInput3)
            Assert.True(nInput3.VarType.StartsWith("uint", StringComparison.OrdinalIgnoreCase))

            Dim nInput4 = FindParam(parameters, "NInput4")
            Assert.NotNull(nInput4)
            Assert.Equal("double", nInput4.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSFloat, nInput4.ParamType)

            ' Listbox params
            Dim lInput1 = FindParam(parameters, "LInput1")
            Assert.NotNull(lInput1)
            Assert.True(lInput1.IsMandatory)
            Assert.True(lInput1.IsSelect)

            Dim lInput2 = FindParam(parameters, "LInput2")
            Assert.NotNull(lInput2)
            Assert.True(lInput2.IsSelect)
            Assert.True(lInput2.IsMultiValued)

            ' Date params
            Dim dInput01 = FindParam(parameters, "DInput01DT")
            Assert.NotNull(dInput01)
            Assert.Equal("datetime", dInput01.VarType)
            Assert.False(dInput01.DirectiveDateTime)

            Dim dInput02 = FindParam(parameters, "DInput02DT")
            Assert.NotNull(dInput02)
            Assert.Equal("datetime", dInput02.VarType)
            Assert.True(dInput02.DirectiveDateTime)

            ' Switch/boolean params
            Dim sw = FindParam(parameters, "Input11Switch")
            Assert.NotNull(sw)
            Assert.Equal("switch", sw.VarType)
            Assert.True(sw.IsMandatory)

            Dim bl = FindParam(parameters, "Input12Bool")
            Assert.NotNull(bl)
            Assert.Equal("boolean", bl.VarType)
        End Sub

#End Region

    End Class
End Namespace

