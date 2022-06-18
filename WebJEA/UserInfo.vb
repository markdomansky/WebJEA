Imports System.DirectoryServices
Imports System.DirectoryServices.AccountManagement
Imports System.DirectoryServices.ActiveDirectory
Imports System.Security.Principal

Public Class UserInfo
    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()
    Private prvPrincipal As System.Security.Principal.IPrincipal

    'Private Declare Auto Function TranslateName Lib "secur32" (accountName As String, nameFormat As EXTENDED_NAME_FORMAT, desiredFormat As EXTENDED_NAME_FORMAT, ByVal translatedName As System.Text.StringBuilder, ByRef userNameSize As Integer) As Byte
    'Private Enum EXTENDED_NAME_FORMAT As Int32
    '    NameUnknown = 0
    '    NameFullyQualifiedDN = 1 'distinguishedname
    '    NameSamCompatible = 2 'domain\user
    '    NameDisplay = 3 'displayname
    '    NameUniqueeId = 6 'guid
    '    NameCanonical = 7 'domain/ou/user
    '    NameUserPrincipal = 8 'user@domain
    '    NameCanonicalEx = 9 'domain/ou\user
    '    NameServicePrincipal = 10 'service/user@domain ??
    '    NameDnsName = 12 'dnsdomain\username
    'End Enum


    Private uname As String
    Private prvSIDs As New List(Of String)
    Private prvDomainSID As String = "-"
    Private prvDomainDNSRoot As String = "-"

    Sub New(curuser As System.Security.Principal.IPrincipal)

        prvPrincipal = curuser
        Dim WinID As System.Security.Principal.WindowsIdentity = prvPrincipal.Identity
        'get username from page request
        dlog.Trace("UserInfo: User: " & WinID.Name)
        'save username 
        uname = WinID.Name

        For Each clm As System.Security.Claims.Claim In WinID.UserClaims
            dlog.Trace("UserInfo: Claims: " & clm.Value)
            'includes the domain\user entry
            prvSIDs.Add(clm.Value)
        Next

        'get domain sid and dnsroot, if they don't exist, use machinename and sid?
        SetMachineProperties()
        SetDomainProperties()

    End Sub

    Private Sub SetMachineProperties()
        'preset to local machine values in case we're not domain joined

        Try
            Dim regpath As String = "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography"
            Dim readValue = My.Computer.Registry.GetValue(regpath, "MachineGuid", "-") 'i think this always fails for permissions
            prvDomainSID = readValue
            prvDomainDNSRoot = Environment.MachineName
        Catch
        End Try

    End Sub

    Private Sub SetDomainProperties()
        Dim DomainSID As SecurityIdentifier = Nothing
        Dim DomainDNSRoot As String = "-"

        Try
            Dim de As DirectoryServices.DirectoryEntry = Domain.GetCurrentDomain.GetDirectoryEntry
            DomainSID = New SecurityIdentifier(DirectCast(de.InvokeGet("objectSID"), Byte()), 0)
            DomainDNSRoot = de.InvokeGet("DomainDNSRoot")

            prvDomainSID = DomainSID.AccountDomainSid.ToString
            prvDomainDNSRoot = DomainDNSRoot
        Catch ex As Exception
        End Try

    End Sub

    'TODO: Add UPN support
    'Public ReadOnly Property UserPrincipalName As String
    '    Get
    '        Dim name As New System.Text.StringBuilder(256)
    '        Dim namelength As Integer
    '        Dim result As Byte = TranslateName(uname, EXTENDED_NAME_FORMAT.NameSamCompatible, EXTENDED_NAME_FORMAT.NameUserPrincipal, name, namelength)
    '        If name.Length > 0 Then
    '            Return name.ToString()
    '        Else
    '            Return uname
    '        End If

    '    End Get
    'End Property

    Public ReadOnly Property MemberOfSIDs As List(Of String)
        Get
            Return prvSIDs
        End Get
    End Property

    Public ReadOnly Property UserName As String
        Get
            Return uname
        End Get
    End Property

    Public Function IsMemberOf(groupSID As String) As Boolean
        If groupSID = "*" Then Return True

        For Each usersid As String In prvSIDs
            If groupSID = usersid Then Return True
        Next

        Return False
    End Function

    Public ReadOnly Property DomainSID As String
        Get
            Return prvDomainSID
        End Get
    End Property

    Public ReadOnly Property DomainDNSRoot As String
        Get
            Return prvDomainDNSRoot
        End Get
    End Property

End Class
