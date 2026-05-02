Imports System
Imports Xunit
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class OutputRendererTests

        Private ReadOnly renderer As New OutputRenderer()

#Region "EncodeOutputTags - anchor tags"

        <Fact>
        Public Sub EncodeOutputTags_AnchorTag_ConvertsToHtml()
            Dim input = "[[a|https://example.com|Click here]]"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Equal("<a href='https://example.com'>Click here</a>", result)
        End Sub

        <Fact>
        Public Sub EncodeOutputTags_MultipleAnchors_ConvertsAll()
            Dim input = "[[a|https://one.com|One]] and [[a|https://two.com|Two]]"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Contains("<a href='https://one.com'>One</a>", result)
            Assert.Contains("<a href='https://two.com'>Two</a>", result)
        End Sub

        <Fact>
        Public Sub EncodeOutputTags_AnchorCaseInsensitive_Converts()
            Dim input = "[[A|https://example.com|Link]]"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Contains("<a href='https://example.com'>Link</a>", result)
        End Sub

#End Region

#Region "EncodeOutputTags - span tags"

        <Fact>
        Public Sub EncodeOutputTags_SpanTag_ConvertsToHtml()
            Dim input = "[[span|myclass|content text]]"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Equal("<span Class='myclass'>content text</span>", result)
        End Sub

        <Fact>
        Public Sub EncodeOutputTags_SpanCaseInsensitive_Converts()
            Dim input = "[[SPAN|highlight|important]]"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Contains("<span Class='highlight'>important</span>", result)
        End Sub

#End Region

#Region "EncodeOutputTags - img tags"

        <Fact>
        Public Sub EncodeOutputTags_ImgTag_ConvertsToHtml()
            Dim input = "[[img|thumbnail|https://example.com/image.png]]"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Equal("<img class='thumbnail' src='https://example.com/image.png' />", result)
        End Sub

        <Fact>
        Public Sub EncodeOutputTags_ImgEmptyClass_ConvertsToHtml()
            Dim input = "[[img||https://example.com/image.png]]"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Equal("<img class='' src='https://example.com/image.png' />", result)
        End Sub

#End Region

#Region "EncodeOutputTags - no tags"

        <Fact>
        Public Sub EncodeOutputTags_NoTags_ReturnsUnchanged()
            Dim input = "Just plain text with no special tags"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Equal(input, result)
        End Sub

        <Fact>
        Public Sub EncodeOutputTags_EmptyString_ReturnsEmpty()
            Dim result = renderer.EncodeOutputTags("")

            Assert.Equal("", result)
        End Sub

        <Fact>
        Public Sub EncodeOutputTags_PartialTag_ReturnsUnchanged()
            Dim input = "[[a|incomplete"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Equal(input, result)
        End Sub

#End Region

#Region "EncodeOutputTags - mixed content"

        <Fact>
        Public Sub EncodeOutputTags_MixedTags_ConvertsAll()
            Dim input = "Text [[a|https://link.com|link]] more [[span|bold|text]] end [[img|pic|https://img.com/x.png]]"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Contains("<a href='https://link.com'>link</a>", result)
            Assert.Contains("<span Class='bold'>text</span>", result)
            Assert.Contains("<img class='pic' src='https://img.com/x.png' />", result)
        End Sub

        <Fact>
        Public Sub EncodeOutputTags_TextAroundTags_PreservesSurroundingText()
            Dim input = "Before [[a|https://example.com|link]] after"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.StartsWith("Before ", result)
            Assert.EndsWith(" after", result)
        End Sub

#End Region

#Region "EncodeOutputTags - XSS vectors"

        <Fact>
        Public Sub EncodeOutputTags_JavascriptInAnchorHref_StillConverts()
            ' This demonstrates the known XSS risk documented in plan.md Step 9
            Dim input = "[[a|javascript:alert(1)|click]]"
            Dim result = renderer.EncodeOutputTags(input)

            Assert.Contains("javascript:alert(1)", result)
        End Sub

#End Region

    End Class
End Namespace
