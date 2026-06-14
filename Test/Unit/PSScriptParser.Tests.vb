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

#End Region

#Region "validate.ps1 - comprehensive template with all param types"

        <Fact>
        Public Sub Validate_ShouldParseFullTemplateWithAllParamTypes()
            Dim parser As New PSScriptParser(GetScriptPath("validate.ps1"))
            Dim parameters = parser.GetParameters()

            ' Comment block
            Assert.Contains("Short 1 line description of what this script does.", parser.Synopsis)
            Assert.Contains("Detailed description", parser.Description)
            Assert.Single(parser.Examples)
            Assert.Contains("ScriptTemplate.ps1", parser.Examples(0))

            ' String params
            Dim p01 = FindParam(parameters, "Input01Mandatory")
            Assert.NotNull(p01)
            Assert.True(p01.IsMandatory)
            Assert.Equal("string", p01.VarType)
            Assert.Contains("computer", p01.HelpMessage.ToLower())

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


#Region "utsp-EmptyScript.ps1 - empty file"

        <Fact>
        Public Sub UtspEmptyScript_ShouldNotBeValidAndHaveNoContent()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-EmptyScript.ps1"))

            Assert.False(parser.IsValid)
            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
            Assert.Empty(parser.GetParameters())
        End Sub

#End Region

#Region "utsp-InvalidScript.ps1 - incomplete comment block"

        <Fact>
        Public Sub UtspInvalidScript_ShouldNotParseIncompleteCommentBlock()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-InvalidScript.ps1"))

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
        End Sub

#End Region

#Region "utsp-noparamblock.ps1 - no param block"

        <Fact>
        Public Sub UtspNoParamBlock_ShouldHaveNoParameters()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-noparamblock.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
            Assert.Empty(parameters)
        End Sub

#End Region

#Region "utsp-noparams.ps1 - empty param block"

        <Fact>
        Public Sub UtspNoParams_ShouldHaveEmptyParameterList()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-noparams.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
            Assert.Empty(parameters)
        End Sub

#End Region

#Region "utsp-boolean.ps1 - boolean type, no default"

        <Fact>
        Public Sub UtspBoolean_ShouldParseBooleanParam()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-boolean.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("boolean", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSBoolean, p.ParamType)
            Assert.False(p.IsMandatory)
            Assert.Null(p.DefaultValue)
        End Sub

#End Region

#Region "utsp-boolean-true.ps1 - boolean type with default $true"

        <Fact>
        Public Sub UtspBooleanTrue_ShouldParseBooleanParamWithDefaultTrue()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-boolean-true.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("boolean", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSBoolean, p.ParamType)
            Assert.Equal(True, p.DefaultValue)
        End Sub

#End Region

#Region "utsp-switch.ps1 - switch type, mandatory"

        <Fact>
        Public Sub UtspSwitch_ShouldParseMandatorySwitchParam()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-switch.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("switch", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSBoolean, p.ParamType)
            Assert.True(p.IsMandatory)
        End Sub

#End Region

#Region "utsp-string.ps1 - string type with multiline directive and default"

        <Fact>
        Public Sub UtspString_ShouldParseStringParamWithMultilineAndDefault()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-string.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("string", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p.ParamType)
            Assert.True(p.DirectiveMultiline)
            Assert.Equal("A", CStr(p.DefaultValue))
        End Sub

#End Region

#Region "utsp-string-array.ps1 - string array with array default"

        <Fact>
        Public Sub UtspStringArray_ShouldParseStringArrayWithArrayDefault()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-string-array.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("string[]", p.VarType)
            Assert.True(p.IsMultiValued)
            Assert.IsType(Of List(Of String))(p.DefaultValue)
            Dim defaultList = DirectCast(p.DefaultValue, List(Of String))
            Assert.Contains("a", defaultList)
            Assert.Contains("d", defaultList)
        End Sub

#End Region

#Region "utsp-int.ps1 - int type"

        <Fact>
        Public Sub UtspInt_ShouldParseIntParam()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-int.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.True(p.VarType.StartsWith("int", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(PSCmdParam.ParameterType.PSInt, p.ParamType)
        End Sub

#End Region

#Region "utsp-int-int32.ps1 - Int32 type"

        <Fact>
        Public Sub UtspIntInt32_ShouldParseInt32Param()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-int-int32.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.True(String.Equals("int32", p.VarType, StringComparison.OrdinalIgnoreCase))
            Assert.Equal(PSCmdParam.ParameterType.PSInt, p.ParamType)
        End Sub

#End Region

#Region "utsp-int-uint16.ps1 - UInt16 type"

        <Fact>
        Public Sub UtspIntUInt16_ShouldParseUInt16Param()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-int-uint16.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.True(p.VarType.StartsWith("uint", StringComparison.OrdinalIgnoreCase))
            Assert.Equal(PSCmdParam.ParameterType.PSInt, p.ParamType)
        End Sub

#End Region

#Region "utsp-int-array.ps1 - int array with ValidateCount and ValidateRange"

        <Fact>
        Public Sub UtspIntArray_ShouldParseIntArrayWithValidations()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-int-array.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Contains("[]", p.VarType)
            Assert.True(p.IsMultiValued)
            Assert.Equal(PSCmdParam.ParameterType.PSInt, p.ParamType)
            Assert.True(p.Validation.Any(Function(v) v.StartsWith("ValidateCount", StringComparison.OrdinalIgnoreCase)))
            Assert.True(p.Validation.Any(Function(v) v.StartsWith("ValidateRange", StringComparison.OrdinalIgnoreCase)))
        End Sub

#End Region

#Region "utsp-float-float.ps1 - float type"

        <Fact>
        Public Sub UtspFloatFloat_ShouldParseFloatParam()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-float-float.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("float", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSFloat, p.ParamType)
        End Sub

#End Region

#Region "utsp-float-double.ps1 - double type"

        <Fact>
        Public Sub UtspFloatDouble_ShouldParseDoubleParam()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-float-double.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("double", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSFloat, p.ParamType)
        End Sub

#End Region

#Region "utsp-float-decimal.ps1 - decimal type"

        <Fact>
        Public Sub UtspFloatDecimal_ShouldParseDecimalParam()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-float-decimal.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            ' decimal is not a recognized type in the parser; VarType is left empty
            Assert.Empty(p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p.ParamType)
        End Sub

#End Region

#Region "utsp-dateonly.ps1 - datetime type without WEBJEA-DateTime directive"

        <Fact>
        Public Sub UtspDateOnly_ShouldParseDateTimeParamWithoutDirective()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-dateonly.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("datetime", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSDate, p.ParamType)
            Assert.False(p.DirectiveDateTime)
        End Sub

#End Region

#Region "utsp-datetime.ps1 - datetime type with WEBJEA-DateTime directive"

        <Fact>
        Public Sub UtspDateTime_ShouldParseDateTimeParamWithDirective()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-datetime.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("datetime", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSDate, p.ParamType)
            Assert.True(p.DirectiveDateTime)
        End Sub

#End Region

#Region "utsp-credential.ps1 - pscredential type (unrecognized, empty VarType)"

        <Fact>
        Public Sub UtspCredential_ShouldParseCredentialAsEmptyVarType()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-cred-credential.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Empty(p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p.ParamType)
        End Sub

#End Region

#Region "utsp-pscredential.ps1 - pscredential type (unrecognized, empty VarType)"

        <Fact>
        Public Sub UtspPsCredential_ShouldParsePsCredentialAsEmptyVarType()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-cred-pscredential.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Empty(p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p.ParamType)
        End Sub

#End Region

#Region "utsp-securestring.ps1 - securestring type (unrecognized, empty VarType)"

        <Fact>
        Public Sub UtspSecureString_ShouldParseSecureStringAsEmptyVarType()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-cred-securestring.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Empty(p.VarType)
        End Sub

#End Region

#Region "utsp-validatecount.ps1 - ValidateCount on string array"

        <Fact>
        Public Sub UtspValidateCount_ShouldParseValidateCountOnStringArray()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-validatecount.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("string[]", p.VarType)
            Assert.True(p.IsMultiValued)
            Assert.True(p.Validation.Any(Function(v) v.StartsWith("ValidateCount", StringComparison.OrdinalIgnoreCase)))
        End Sub

#End Region

#Region "utsp-validatelength.ps1 - ValidateLength on string"

        <Fact>
        Public Sub UtspValidateLength_ShouldParseValidateLengthOnString()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-validatelength.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("string", p.VarType)
            Assert.True(p.Validation.Any(Function(v) v.StartsWith("ValidateLength", StringComparison.OrdinalIgnoreCase)))
        End Sub

#End Region

#Region "utsp-validatenotnull.ps1 - ValidateNotNull on string"

        <Fact>
        Public Sub UtspValidateNotNull_ShouldParseValidateNotNull()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-validatenotnull.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("string", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p.ParamType)
            Assert.True(p.Validation.Any(Function(v) v.StartsWith("ValidateNotNull", StringComparison.OrdinalIgnoreCase)))
        End Sub

#End Region

#Region "utsp-validatenotnullorempty.ps1 - ValidateNotNullOrEmpty on string"

        <Fact>
        Public Sub UtspValidateNotNullOrEmpty_ShouldParseValidateNotNullOrEmpty()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-validatenotnullorempty.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("string", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p.ParamType)
            Assert.True(p.Validation.Any(Function(v) v.StartsWith("ValidateNotNullOrEmpty", StringComparison.OrdinalIgnoreCase)))
        End Sub

#End Region

#Region "utsp-validatepattern.ps1 - ValidatePattern on untyped param"

        <Fact>
        Public Sub UtspValidatePattern_ShouldParseValidatePattern()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-validatepattern.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.True(p.Validation.Any(Function(v) v.StartsWith("ValidatePattern", StringComparison.OrdinalIgnoreCase)))
        End Sub

#End Region

#Region "utsp-validaterange.ps1 - ValidateRange on untyped param"

        <Fact>
        Public Sub UtspValidateRange_ShouldParseValidateRange()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-validaterange.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.True(p.Validation.Any(Function(v) v.StartsWith("ValidateRange", StringComparison.OrdinalIgnoreCase)))
        End Sub

#End Region

#Region "utsp-validatescript.ps1 - ValidateScript on untyped param"

        <Fact>
        Public Sub UtspValidateScript_ShouldParseValidateScriptParam()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-validatescript.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p.ParamType)
        End Sub

#End Region

#Region "utsp-validateset.ps1 - ValidateSet on string param"

        <Fact>
        Public Sub UtspValidateSet_ShouldParseValidateSetAsSelect()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-validateset.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("string", p.VarType)
            Assert.True(p.IsSelect)
            Assert.False(p.IsMultiValued)
            Assert.Contains("Input", p.AllowedValues)
            Assert.Contains("Output", p.AllowedValues)
            Assert.Contains("Both", p.AllowedValues)
        End Sub

#End Region

#Region "utsp-webjeahostname.ps1 - WebJEAHostname string param"

        <Fact>
        Public Sub UtspWebJEAHostname_ShouldParseWebJEAHostnameParam()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-webjeahostname.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "WebJEAHostname")
            Assert.NotNull(p)
            Assert.Equal("string", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p.ParamType)
        End Sub

#End Region

#Region "utsp-webjeausername.ps1 - WebJEAUsername string param"

        <Fact>
        Public Sub UtspWebJEAUsername_ShouldParseWebJEAUsernameParam()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-webjeausername.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "WebJEAUsername")
            Assert.NotNull(p)
            Assert.Equal("string", p.VarType)
            Assert.Equal(PSCmdParam.ParameterType.PSString, p.ParamType)
        End Sub

#End Region

#Region "utsp-help-synopsis.ps1 - synopsis in comment block"

        <Fact>
        Public Sub UtspHelpSynopsis_ShouldParseSynopsis()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-help-synopsis.ps1"))

            Assert.Equal("Synopsis String Check", parser.Synopsis)
            Assert.Empty(parser.Description)
            Assert.Empty(parser.Examples)
        End Sub

#End Region

#Region "utsp-help-description.ps1 - description in comment block"

        <Fact>
        Public Sub UtspHelpDescription_ShouldParseDescription()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-help-description.ps1"))

            Assert.Empty(parser.Synopsis)
            Assert.Contains("Description String Check", parser.Description)
            Assert.Empty(parser.Examples)
        End Sub

#End Region

#Region "utsp-help-parameter.ps1 - parameter help from comment block"

        <Fact>
        Public Sub UtspHelpParameter_ShouldParseParameterHelpDetail()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-help-parameter.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Contains("Var description", p.HelpDetail)
        End Sub

#End Region

#Region "utsp-help-helpmessage.ps1 - HelpMessage attribute on param"

        <Fact>
        Public Sub UtspHelpHelpMessage_ShouldParseHelpMessageAttribute()
            Dim parser As New PSScriptParser(GetScriptPath("utsp-help-helpmessage.ps1"))
            Dim parameters = parser.GetParameters()

            Assert.Empty(parser.Synopsis)
            Assert.Empty(parser.Description)

            Assert.Single(parameters)
            Dim p = FindParam(parameters, "Var")
            Assert.NotNull(p)
            Assert.Equal("Enter Value", p.HelpMessage)
        End Sub

#End Region

    End Class
End Namespace

