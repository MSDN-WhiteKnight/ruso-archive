
# Настройка .NET Framework для работы с TLS 1.2

StackExchange API с февраля 2020 г. поддерживает только протокол TLS 1.2. Для его включения необходимо установить в реестре следующие значения:

**SystemDefaultTlsVersions**

```
[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v4.0.30319]
"SystemDefaultTlsVersions"=dword:00000001
```

Для Windows 7 также необходимо добавить раздел **TLS 1.2** в протоколах Schannel

```
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2]

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client]
"Enabled"=dword:00000001
"DisabledByDefault"=dword:00000000

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Server]
"DisabledByDefault"=dword:00000000
"Enabled"=dword:00000001
```

Ссылки:

- [TLS 1.0 and TLS 1.1 removal for Stack Exchange services](https://meta.stackexchange.com/questions/343302/tls-1-0-and-tls-1-1-removal-for-stack-exchange-services)
- [Transport Layer Security (TLS) best practices with the .NET Framework](https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls)
- [Transport Layer Security (TLS) registry settings](https://docs.microsoft.com/en-us/windows-server/security/tls/tls-registry-settings#tls-12)
