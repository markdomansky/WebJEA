Module modGlobal


    Public Enum globalKeys
        aws_enabled
        aws_key
        aws_keysec
        aws_serviceUrl
        aws_queueUrl
    End Enum
    Public globalSettings As New Dictionary(Of String, String) From {
        {globalKeys.aws_enabled, True},
        {globalKeys.aws_key, "AKIAJ6X6FWQ3UWAOQL7Q"},
        {globalKeys.aws_keysec, "o2xPU115uqFZA6OotH9IWfWXGATU17wtm7vQRev6"},
        {globalKeys.aws_serviceUrl, "https://sqs.us-west-2.amazonaws.com"},
        {globalKeys.aws_queueUrl, "https://sqs.us-west-2.amazonaws.com/777088161147/webjea-telemetry"}}


End Module
