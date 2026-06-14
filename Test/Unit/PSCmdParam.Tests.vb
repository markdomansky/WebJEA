Imports System
Imports Xunit
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class PSCmdParamTests

#Region "ParamType classification"

        <Theory>
        <InlineData("string", PSCmdParam.ParameterType.PSString)>
        <InlineData("string[]", PSCmdParam.ParameterType.PSString)>
        <InlineData("String", PSCmdParam.ParameterType.PSString)>
        <InlineData("datetime", PSCmdParam.ParameterType.PSDate)>
        <InlineData("int", PSCmdParam.ParameterType.PSInt)>
        <InlineData("int32", PSCmdParam.ParameterType.PSInt)>
        <InlineData("int64", PSCmdParam.ParameterType.PSInt)>
        <InlineData("int[]", PSCmdParam.ParameterType.PSInt)>
        <InlineData("uint32", PSCmdParam.ParameterType.PSInt)>
        <InlineData("byte", PSCmdParam.ParameterType.PSInt)>
        <InlineData("long", PSCmdParam.ParameterType.PSInt)>
        <InlineData("single", PSCmdParam.ParameterType.PSFloat)>
        <InlineData("double", PSCmdParam.ParameterType.PSFloat)>
        <InlineData("float", PSCmdParam.ParameterType.PSFloat)>
        <InlineData("boolean", PSCmdParam.ParameterType.PSBoolean)>
        <InlineData("bool", PSCmdParam.ParameterType.PSBoolean)>
        <InlineData("switch", PSCmdParam.ParameterType.PSBoolean)>
        <InlineData("", PSCmdParam.ParameterType.PSString)>
        Public Sub ParamType_ClassifiesCorrectly(varType As String, expected As PSCmdParam.ParameterType)
            Dim param As New PSCmdParam()
            param.VarType = varType

            Assert.Equal(expected, param.ParamType)
        End Sub

        <Fact>
        Public Sub ParamType_UnknownType_DefaultsToString()
            Dim param As New PSCmdParam()
            param.VarType = "pscredential"

            Assert.Equal(PSCmdParam.ParameterType.PSString, param.ParamType)
        End Sub

#End Region

#Region "IsMandatory"

        <Fact>
        Public Sub IsMandatory_NoValidation_ReturnsFalse()
            Dim param As New PSCmdParam()
            Assert.False(param.IsMandatory)
        End Sub

        <Fact>
        Public Sub IsMandatory_HasMandatoryValidation_ReturnsTrue()
            Dim param As New PSCmdParam()
            param.AddValidation("Mandatory")

            Assert.True(param.IsMandatory)
        End Sub

        <Fact>
        Public Sub IsMandatory_CaseInsensitive()
            Dim param As New PSCmdParam()
            param.AddValidation("MANDATORY")

            Assert.True(param.IsMandatory)
        End Sub

        <Fact>
        Public Sub IsMandatory_OtherValidation_ReturnsFalse()
            Dim param As New PSCmdParam()
            param.AddValidation("ValidateLength(1,50)")

            Assert.False(param.IsMandatory)
        End Sub

#End Region

#Region "IsMultiValued"

        <Fact>
        Public Sub IsMultiValued_ArrayType_ReturnsTrue()
            Dim param As New PSCmdParam()
            param.VarType = "string[]"

            Assert.True(param.IsMultiValued)
        End Sub

        <Fact>
        Public Sub IsMultiValued_ScalarType_ReturnsFalse()
            Dim param As New PSCmdParam()
            param.VarType = "string"

            Assert.False(param.IsMultiValued)
        End Sub

        <Fact>
        Public Sub IsMultiValued_MultilineDirective_ReturnsFalse()
            Dim param As New PSCmdParam()
            param.DirectiveMultiline = True

            Assert.False(param.IsMultiValued)
        End Sub

        <Fact>
        Public Sub IsMultiValued_IntArray_ReturnsTrue()
            Dim param As New PSCmdParam()
            param.VarType = "int[]"

            Assert.True(param.IsMultiValued)
        End Sub

#End Region

#Region "IsSelect and AllowedValues"

        <Fact>
        Public Sub IsSelect_NoValidateSet_ReturnsFalse()
            Dim param As New PSCmdParam()
            Assert.False(param.IsSelect)
        End Sub

        <Fact>
        Public Sub IsSelect_WithValidateSet_ReturnsTrue()
            Dim param As New PSCmdParam()
            param.AddValidation("ValidateSet('A','B','C')")

            Assert.True(param.IsSelect)
        End Sub

        <Fact>
        Public Sub AllowedValues_WithValidateSet_ReturnsOptions()
            Dim param As New PSCmdParam()
            param.AddValidation("ValidateSet('Input','Output','Both')")

            Dim allowed = param.AllowedValues
            Assert.NotNull(allowed)
            Assert.Contains("Input", allowed)
            Assert.Contains("Output", allowed)
            Assert.Contains("Both", allowed)
        End Sub

        <Fact>
        Public Sub AllowedValues_NoValidateSet_ReturnsNothing()
            Dim param As New PSCmdParam()
            Assert.Null(param.AllowedValues)
        End Sub

#End Region

#Region "AddValidation"

        <Fact>
        Public Sub AddValidation_ValidateLength_Added()
            Dim param As New PSCmdParam()
            param.AddValidation("ValidateLength(1,50)")

            Assert.Single(param.Validation)
            Assert.Contains("ValidateLength(1,50)", param.Validation)
        End Sub

        <Fact>
        Public Sub AddValidation_Mandatory_Added()
            Dim param As New PSCmdParam()
            param.AddValidation("Mandatory")

            Assert.Single(param.Validation)
        End Sub

        <Fact>
        Public Sub AddValidation_DuplicatesIgnored()
            Dim param As New PSCmdParam()
            param.AddValidation("Mandatory")
            param.AddValidation("Mandatory")

            Assert.Single(param.Validation)
        End Sub

        <Fact>
        Public Sub AddValidation_AliasIsIgnored()
            Dim param As New PSCmdParam()
            param.AddValidation("Alias('myalias')")

            Assert.Empty(param.Validation)
        End Sub

        <Fact>
        Public Sub AddValidation_AllowNull_Added()
            Dim param As New PSCmdParam()
            param.AddValidation("AllowNull")

            Assert.Single(param.Validation)
        End Sub

#End Region

#Region "Clone"

        <Fact>
        Public Sub Clone_CopiesAllProperties()
            Dim original As New PSCmdParam()
            original.Name = "TestParam"
            original.HelpMessage = "Help"
            original.HelpDetail = "Detail"
            original.VarType = "string"
            original.DirectiveMultiline = True
            original.DirectiveDateTime = True
            original.DefaultValue = "default"
            original.AddValidation("Mandatory")
            original.AddValidation("ValidateLength(1,50)")

            Dim clone = original.Clone()

            Assert.Equal("TestParam", clone.Name)
            Assert.Equal("Help", clone.HelpMessage)
            Assert.Equal("Detail", clone.HelpDetail)
            Assert.Equal("string", clone.VarType)
            Assert.True(clone.DirectiveMultiline)
            Assert.True(clone.DirectiveDateTime)
            Assert.Equal("default", CStr(clone.DefaultValue))
            Assert.True(clone.IsMandatory)
            Assert.Equal(2, clone.Validation.Count)
        End Sub

        <Fact>
        Public Sub Clone_IsIndependentCopy()
            Dim original As New PSCmdParam()
            original.Name = "Original"
            original.AddValidation("Mandatory")

            Dim clone = original.Clone()
            clone.Name = "Cloned"
            clone.AddValidation("ValidateLength(1,50)")

            Assert.Equal("Original", original.Name)
            Assert.Single(original.Validation)
        End Sub

#End Region

#Region "MergeUnder"

        <Fact>
        Public Sub MergeUnder_DoesNotOverwriteExistingValues()
            Dim target As New PSCmdParam()
            target.Name = "Target"
            target.HelpMessage = "Existing Help"
            target.VarType = "int"

            Dim source As New PSCmdParam()
            source.HelpMessage = "Source Help"
            source.HelpDetail = "Source Detail"
            source.VarType = "string"

            target.MergeUnder(source)

            Assert.Equal("Existing Help", target.HelpMessage)
            Assert.Equal("Source Detail", target.HelpDetail)
            Assert.Equal("int", target.VarType)
        End Sub

        <Fact>
        Public Sub MergeUnder_FillsEmptyValues()
            Dim target As New PSCmdParam()
            target.Name = "Target"

            Dim source As New PSCmdParam()
            source.HelpMessage = "Source Help"
            source.HelpDetail = "Source Detail"
            source.VarType = "string"
            source.DefaultValue = "default"

            target.MergeUnder(source)

            Assert.Equal("Source Help", target.HelpMessage)
            Assert.Equal("Source Detail", target.HelpDetail)
            Assert.Equal("string", target.VarType)
            Assert.Equal("default", CStr(target.DefaultValue))
        End Sub

        <Fact>
        Public Sub MergeUnder_MergesValidation()
            Dim target As New PSCmdParam()
            target.AddValidation("Mandatory")

            Dim source As New PSCmdParam()
            source.AddValidation("ValidateLength(1,50)")

            target.MergeUnder(source)

            Assert.Equal(2, target.Validation.Count)
        End Sub

#End Region

#Region "MergeOver"

        <Fact>
        Public Sub MergeOver_OverwritesWithSourceValues()
            Dim target As New PSCmdParam()
            target.Name = "Target"
            target.HelpMessage = "Original"

            Dim source As New PSCmdParam()
            source.HelpMessage = "Override"

            target.MergeOver(source)

            Assert.Equal("Override", target.HelpMessage)
        End Sub

        <Fact>
        Public Sub MergeOver_DoesNotOverwriteWithEmptySource()
            Dim target As New PSCmdParam()
            target.HelpMessage = "Keep This"

            Dim source As New PSCmdParam()

            target.MergeOver(source)

            Assert.Equal("Keep This", target.HelpMessage)
        End Sub

        <Fact>
        Public Sub MergeOver_MergesValidation()
            Dim target As New PSCmdParam()
            target.AddValidation("Mandatory")

            Dim source As New PSCmdParam()
            source.AddValidation("ValidateRange(1,100)")

            target.MergeOver(source)

            Assert.Equal(2, target.Validation.Count)
        End Sub

#End Region

#Region "FieldName"

        <Fact>
        Public Sub FieldName_ReturnsName()
            Dim param As New PSCmdParam()
            param.Name = "MyParam"

            Assert.Equal("MyParam", param.FieldName)
        End Sub

#End Region

#Region "ValidationObjects"

        <Fact>
        Public Sub ValidationObjects_ReturnsValidParsedObjects()
            Dim param As New PSCmdParam()
            param.AddValidation("Mandatory")
            param.AddValidation("ValidateLength(1,50)")
            param.AddValidation("ValidateRange(0,100)")

            Dim valObjs = param.ValidationObjects
            Assert.Equal(3, valObjs.Count)
            Assert.All(valObjs, Sub(v) Assert.True(v.IsValid))
        End Sub

        <Fact>
        Public Sub ValidationObjects_EmptyValidation_ReturnsEmptyList()
            Dim param As New PSCmdParam()

            Dim valObjs = param.ValidationObjects
            Assert.Empty(valObjs)
        End Sub

#End Region

    End Class
End Namespace
