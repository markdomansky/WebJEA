Imports System
Imports Xunit
Imports System.IO
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class HelpersTests

#Region "CoalesceString"

        <Fact>
        Public Sub CoalesceString_AllNull_ReturnsNothing()
            Dim result = CoalesceString(Nothing, Nothing, Nothing)
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub CoalesceString_FirstNotNull_ReturnsFirst()
            Dim result = CoalesceString("first", "second", "third")
            Assert.Equal("first", result)
        End Sub

        <Fact>
        Public Sub CoalesceString_FirstNullSecondValid_ReturnsSecond()
            Dim result = CoalesceString(Nothing, "second", "third")
            Assert.Equal("second", result)
        End Sub

        <Fact>
        Public Sub CoalesceString_OnlyLastNotNull_ReturnsLast()
            Dim result = CoalesceString(Nothing, Nothing, "third")
            Assert.Equal("third", result)
        End Sub

        <Fact>
        Public Sub CoalesceString_EmptyStringIsNotNull_ReturnsEmpty()
            Dim result = CoalesceString("", "second")
            Assert.Equal("", result)
        End Sub

        <Fact>
        Public Sub CoalesceString_NoArguments_ReturnsNothing()
            Dim result = CoalesceString()
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub CoalesceString_SingleValue_ReturnsThatValue()
            Dim result = CoalesceString("only")
            Assert.Equal("only", result)
        End Sub

#End Region

#Region "StringHash256"

        <Fact>
        Public Sub StringHash256_SameInput_ReturnsSameHash()
            Dim hash1 = StringHash256("test input")
            Dim hash2 = StringHash256("test input")
            Assert.Equal(hash1, hash2)
        End Sub

        <Fact>
        Public Sub StringHash256_DifferentInput_ReturnsDifferentHash()
            Dim hash1 = StringHash256("input one")
            Dim hash2 = StringHash256("input two")
            Assert.NotEqual(hash1, hash2)
        End Sub

        <Fact>
        Public Sub StringHash256_ReturnsHexString()
            Dim hash = StringHash256("test")
            Assert.Matches("^[0-9A-F]+$", hash)
        End Sub

        <Fact>
        Public Sub StringHash256_Returns64CharHash()
            Dim hash = StringHash256("test")
            Assert.Equal(64, hash.Length)
        End Sub

        <Fact>
        Public Sub StringHash256_EmptyString_ReturnsValidHash()
            Dim hash = StringHash256("")
            Assert.NotNull(hash)
            Assert.Equal(64, hash.Length)
        End Sub

#End Region

#Region "GetFileContent"

        <Fact>
        Public Sub GetFileContent_NonExistentFile_ReturnsNothing()
            Dim result = GetFileContent("C:\nonexistent\fake_file_12345.txt")
            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub GetFileContent_ExistingFile_ReturnsContent()
            Dim tempFile = Path.GetTempFileName()
            Try
                File.WriteAllText(tempFile, "hello world")
                Dim result = GetFileContent(tempFile)
                Assert.Equal("hello world", result)
            Finally
                File.Delete(tempFile)
            End Try
        End Sub

        <Fact>
        Public Sub GetFileContent_EmptyFile_ReturnsEmptyString()
            Dim tempFile = Path.GetTempFileName()
            Try
                Dim result = GetFileContent(tempFile)
                Assert.Equal("", result)
            Finally
                File.Delete(tempFile)
            End Try
        End Sub

        <Fact>
        Public Sub GetFileContent_UnicodeContent_ReturnsCorrectly()
            Dim tempFile = Path.GetTempFileName()
            Try
                File.WriteAllText(tempFile, "café résumé 日本語")
                Dim result = GetFileContent(tempFile)
                Assert.Equal("café résumé 日本語", result)
            Finally
                File.Delete(tempFile)
            End Try
        End Sub

#End Region

    End Class
End Namespace
