' Copy this file to AwsSecrets.vb and fill in your values.
' AwsSecrets.vb is excluded from source control via .gitignore.
' During CI builds, this template is processed automatically with values from GitHub secrets.
Module AwsSecrets
    Public Const Enabled As String = "True"
    Public Const Key As String = "{{AWS_KEY}}"
    Public Const KeySec As String = "{{AWS_KEYSEC}}"
    Public Const QueueUrl As String = "{{AWS_QUEUE_URL}}"
End Module
