Module modGlobal

    Public dlog As NLog.Logger
    Public grpfinder As New GroupFinder
    Public uinfo As UserInfo
    Public cfg As WebJEA.Config

    Public Enum globalKeys
        aws_enabled
        aws_key
        aws_keysec
        aws_serviceUrl
        aws_queueUrl
    End Enum
    Public globalSettings As New Dictionary(Of String, String) From {
		{globalKeys.aws_enabled, False},
        {globalKeys.aws_key, ""},
        {globalKeys.aws_keysec, ""},
        {globalKeys.aws_serviceUrl, ""},
        {globalKeys.aws_queueUrl, ""}}


End Module
