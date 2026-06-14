Imports System
Imports Xunit
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class PSCmdParamValTests

#Region "Mandatory"

        <Fact>
        Public Sub Mandatory_ParsesCorrectly()
            Dim val As New PSCmdParamVal("Mandatory")

            Assert.Equal(PSCmdParamVal.ValType.Mandatory, val.Type)
            Assert.True(val.IsValid)
        End Sub

#End Region

#Region "ValidateLength"

        <Fact>
        Public Sub ValidateLength_ParsesLimits()
            Dim val As New PSCmdParamVal("ValidateLength(1,50)")

            Assert.Equal(PSCmdParamVal.ValType.Length, val.Type)
            Assert.Equal(1, val.LowerLimit)
            Assert.Equal(50, val.UpperLimit)
            Assert.True(val.IsValid)
        End Sub

        <Fact>
        Public Sub ValidateLength_WithSpaces_ParsesCorrectly()
            Dim val As New PSCmdParamVal("ValidateLength( 5 , 100 )")

            Assert.Equal(PSCmdParamVal.ValType.Length, val.Type)
            Assert.Equal(5, val.LowerLimit)
            Assert.Equal(100, val.UpperLimit)
        End Sub

#End Region

#Region "ValidateRange"

        <Fact>
        Public Sub ValidateRange_ParsesLimits()
            Dim val As New PSCmdParamVal("ValidateRange(0,999)")

            Assert.Equal(PSCmdParamVal.ValType.Range, val.Type)
            Assert.Equal(0, val.LowerLimit)
            Assert.Equal(999, val.UpperLimit)
            Assert.True(val.IsValid)
        End Sub

#End Region

#Region "ValidatePattern"

        <Fact>
        Public Sub ValidatePattern_ParsesPattern()
            Dim val As New PSCmdParamVal("ValidatePattern('^[a-z]+$')")

            Assert.Equal(PSCmdParamVal.ValType.Pattern, val.Type)
            Assert.Equal("^[a-z]+$", val.Pattern)
            Assert.True(val.IsValid)
        End Sub

        <Fact>
        Public Sub ValidatePattern_DoubleQuotes_ParsesPattern()
            Dim val As New PSCmdParamVal("ValidatePattern(""^\d+$"")")

            Assert.Equal(PSCmdParamVal.ValType.Pattern, val.Type)
            Assert.Equal("^\d+$", val.Pattern)
        End Sub

#End Region

#Region "ValidateCount"

        <Fact>
        Public Sub ValidateCount_ParsesLimits()
            Dim val As New PSCmdParamVal("ValidateCount(1,5)")

            Assert.Equal(PSCmdParamVal.ValType.Count, val.Type)
            Assert.Equal(1, val.LowerLimit)
            Assert.Equal(5, val.UpperLimit)
            Assert.True(val.IsValid)
        End Sub

#End Region

#Region "ValidateSet"

        <Fact>
        Public Sub ValidateSet_ParsesOptions()
            Dim val As New PSCmdParamVal("ValidateSet('A','B','C')")

            Assert.Equal(PSCmdParamVal.ValType.SetCol, val.Type)
            Assert.True(val.IsValid)
            Assert.Equal(3, val.Options.Count)
            Assert.Contains("A", val.Options)
            Assert.Contains("B", val.Options)
            Assert.Contains("C", val.Options)
        End Sub

        <Fact>
        Public Sub ValidateSet_DoubleQuotes_ParsesOptions()
            Dim val As New PSCmdParamVal("ValidateSet(""Input"",""Output"",""Both"")")

            Assert.Equal(PSCmdParamVal.ValType.SetCol, val.Type)
            Assert.Equal(3, val.Options.Count)
            Assert.Contains("Input", val.Options)
            Assert.Contains("Output", val.Options)
            Assert.Contains("Both", val.Options)
        End Sub

        <Fact>
        Public Sub ValidateSet_SingleOption_ParsesCorrectly()
            Dim val As New PSCmdParamVal("ValidateSet('Only')")

            Assert.Equal(PSCmdParamVal.ValType.SetCol, val.Type)
            Assert.Single(val.Options)
            Assert.Contains("Only", val.Options)
        End Sub

#End Region

#Region "ValidateNotNull / ValidateNotNullOrEmpty"

        <Fact>
        Public Sub ValidateNotNull_IsValid()
            Dim val As New PSCmdParamVal("ValidateNotNull")
            Assert.Equal(PSCmdParamVal.ValType.NotNull, val.Type)
            Assert.True(val.IsValid)
        End Sub

        <Fact>
        Public Sub ValidateNotNullOrEmpty_IsValid()
            Dim val As New PSCmdParamVal("ValidateNotNullOrEmpty")
            Assert.Equal(PSCmdParamVal.ValType.NotNullOrEmpty, val.Type)
            Assert.True(val.IsValid)
        End Sub

#End Region

#Region "Unsupported/ignored rules"

        <Fact>
        Public Sub ValidateScript_IsNotValid()
            Dim val As New PSCmdParamVal("ValidateScript({$_ -gt 0})")
            Assert.Equal(PSCmdParamVal.ValType.Err, val.Type)
            Assert.False(val.IsValid)
        End Sub

        <Fact>
        Public Sub AllowNull_IsNotValid()
            Dim val As New PSCmdParamVal("AllowNull")
            Assert.Equal(PSCmdParamVal.ValType.Err, val.Type)
            Assert.False(val.IsValid)
        End Sub

        <Fact>
        Public Sub AllowEmptyString_IsNotValid()
            Dim val As New PSCmdParamVal("AllowEmptyString")
            Assert.Equal(PSCmdParamVal.ValType.Err, val.Type)
            Assert.False(val.IsValid)
        End Sub

        <Fact>
        Public Sub AllowEmptyCollection_IsNotValid()
            Dim val As New PSCmdParamVal("AllowEmptyCollection")
            Assert.Equal(PSCmdParamVal.ValType.Err, val.Type)
            Assert.False(val.IsValid)
        End Sub

#End Region

#Region "Empty parentheses removal"

        <Fact>
        Public Sub EmptyParentheses_RemovedBeforeParsing()
            Dim val As New PSCmdParamVal("Mandatory()")

            Assert.Equal(PSCmdParamVal.ValType.Mandatory, val.Type)
            Assert.True(val.IsValid)
        End Sub

#End Region

#Region "Rule property"

        <Fact>
        Public Sub Rule_ReturnsOriginalRule()
            Dim val As New PSCmdParamVal("ValidateRange(1,100)")
            Assert.Equal("ValidateRange(1,100)", val.Rule)
        End Sub

        <Fact>
        Public Sub Rule_EmptyParensRemoved_ReturnsCleanedRule()
            Dim val As New PSCmdParamVal("Mandatory()")
            Assert.Equal("Mandatory", val.Rule)
        End Sub

#End Region

    End Class
End Namespace
