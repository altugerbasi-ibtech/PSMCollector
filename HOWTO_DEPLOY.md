# PSMCollector Offline Deployment Rehberi

Bu doküman, projeyi GitHub'dan indiren bir kişinin uygulamayı internete kapalı veya doğrudan yönetim erişimi bulunmayan sunuculara kurması için hazırlanmıştır.

Temel varsayım şudur:

- Kaynak kod, Visual Studio ve .NET SDK bulunan bir geliştirme/build bilgisayarına indirilir.
- Build bilgisayarından hedef sunuculara PowerShell Remoting, RDP, WMI, SQL veya IIS yönetim erişimi yoktur.
- Build bilgisayarı ile kurulum ekipleri arasındaki tek aktarım kanalı bir file share'dir.
- Sunucu üzerindeki komutları ilgili sunucunun yöneticisi, o sunucuda yerel olarak çalıştırır.
- Gerçek connection string, sertifika, domain, gMSA ve AD grup bilgileri GitHub repository'sine yazılmaz.

Bu rehberdeki örnek ortam adları şunlardır:

| Rol | Örnek |
|---|---|
| Uygulama/IIS ve collector sunucusu | `myapp01.ae.local` |
| Merkezi envanter SQL Server | `mydb01.ae.local,1433` |
| Merkezi veritabanı | `PSMEnvanter` |
| Web ve Database Collector gMSA | `AE\gmsaSPWeb$` |
| IIS Collector gMSA | `AE\gmsaSPWorker$` |
| Web adresi | `https://psmcollector.ae.local:8080` |

Kendi ortamınızda bütün örnek adları değiştirin.

## 1. Mimari ve kurulum sorumlulukları

Üç ayrı ekip/rol bulunabilir:

1. **Build sorumlusu**
   - GitHub kaynak kodunu indirir.
   - Restore, build ve test çalıştırır.
   - Offline deploy paketlerini oluşturur.
   - Paketleri file share'e bırakır.

2. **Database yöneticisi**
   - Merkezi `PSMEnvanter` veritabanını oluşturur.
   - Şema scriptini uygular.
   - Collector gMSA login ve minimum veritabanı izinlerini tanımlar.
   - İzlenecek SQL Server'larda Database Collector hesabına DMV okuma izni verir.

3. **Windows/IIS yöneticisi**
   - Offline paketleri file share'den uygulama sunucusuna kopyalar.
   - gMSA önkoşullarını doğrular.
   - IIS sitesini ve Windows Service'leri kurar.
   - Sertifika, DNS, SPN, Windows Authentication ve firewall ayarlarını tamamlar.

Build bilgisayarından sunuculara yönetim bağlantısı gerekmez.

## 2. Gerekli offline malzemeler

Kurulum başlamadan önce aşağıdaki dosyalar file share üzerinde bulunmalıdır:

```text
\\fileserver\software\PSMCollector\1.0.0\
  Web\
  IisCollector\
  DatabaseCollector\
  Database\
    PSMEnvanter.sql
    Permissions-Central.sql
    Permissions-TargetSql.sql
  Prerequisites\
    dotnet-hosting-10.x-win.exe
  Checksums\
    SHA256SUMS.txt
  HOWTO_DEPLOY.md
```

`dotnet-hosting-10.x-win.exe` dosyasını Microsoft'un resmi kaynağından, internete erişebilen güvenilir bir bilgisayarda önceden indirin. Kurulum sunucusunda internetten paket indirmeyin.

Collector paketleri self-contained üretilecekse collector sunucusunda ayrıca .NET Runtime gerekmez. IIS üzerinde ASP.NET Core çalıştırmak için yine de IIS Hosting Bundle/ASP.NET Core Module kurulmalıdır.

## 3. Build bilgisayarı önkoşulları

Build bilgisayarında:

- Visual Studio 2022'nin .NET 10 SDK ile uyumlu güncel sürümü veya daha yeni desteklenen Visual Studio,
- `.NET 10 SDK`,
- Git,
- PowerShell 5.1 veya PowerShell 7,
- NuGet paketlerine erişim veya önceden hazırlanmış offline NuGet feed

bulunmalıdır.

Kontrol:

```powershell
git --version
dotnet --info
dotnet --list-sdks
```

Çıktıda `10.0.x` SDK görülmelidir.

## 4. Kaynak kodun alınması

Git kullanılıyorsa:

```powershell
git clone <REPOSITORY_URL> C:\Build\PSMCollector
Set-Location C:\Build\PSMCollector
git status
```

ZIP indirildiyse arşivi `C:\Build\PSMCollector` gibi kısa ve yazılabilir bir dizine açın.

Dağıtılacak commit'i kaydedin:

```powershell
git rev-parse HEAD
git log -1 --oneline
```

Commit kimliğini paket sürüm notuna ekleyin.

## 5. NuGet restore seçenekleri

### 5.1 Build bilgisayarında internet varsa

```powershell
dotnet restore .\IisSqlConnectionInventory.slnx --configfile .\NuGet.Config
```

### 5.2 Build bilgisayarı da internete kapalıysa

Repository gerekli sürüm kilitli `.nupkg` dosyalarını `offline-nuget` dizininde içerir. Ek feed hazırlamadan:

```powershell
.\scripts\Build-Offline.ps1
```

çalıştırılabilir. Script solution restore, `win-x64` collector runtime restore, `dotnet-ef` tool restore, build ve test adımlarını repository içindeki `NuGet.Offline.Config` ile gerçekleştirir.

Manuel restore gerekiyorsa:

```powershell
dotnet restore .\IisSqlConnectionInventory.slnx --configfile .\NuGet.Offline.Config
```

Bu yaklaşım için yine de uyumlu .NET 10 SDK'nın build bilgisayarında offline olarak kurulmuş olması gerekir. NuGet paketlerinin repository'de bulunması .NET SDK kurulumunun yerini almaz.

## 6. Build ve test

```powershell
dotnet build .\IisSqlConnectionInventory.slnx -c Release --no-restore
dotnet test .\IisSqlConnectionInventory.slnx -c Release --no-build --no-restore
```

Beklenen sonuç:

- Build error: `0`
- Mümkünse warning: `0`
- Tüm unit testler başarılı

Başarısız build veya test çıktısı deploy edilmemelidir.

## 7. Offline publish paketlerinin üretilmesi

Temiz bir staging dizini oluşturun:

```powershell
$PackageRoot = 'C:\BuildOutput\PSMCollector\1.0.0'
New-Item -ItemType Directory -Path $PackageRoot -Force
New-Item -ItemType Directory -Path "$PackageRoot\Web" -Force
New-Item -ItemType Directory -Path "$PackageRoot\IisCollector" -Force
New-Item -ItemType Directory -Path "$PackageRoot\DatabaseCollector" -Force
New-Item -ItemType Directory -Path "$PackageRoot\Database" -Force
```

### 7.1 Web paketi

Framework-dependent web publish:

```powershell
dotnet publish .\src\Inventory.Web\Inventory.Web.csproj `
  -c Release `
  --no-restore `
  -o "$PackageRoot\Web"
```

Bu paket için uygulama sunucusunda .NET 10 Hosting Bundle gerekir.

### 7.2 IIS Collector paketi

Sunucuda ayrıca .NET Runtime gerektirmeyen self-contained paket:

```powershell
dotnet publish .\src\Inventory.IisCollector\Inventory.IisCollector.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  --no-restore `
  -o "$PackageRoot\IisCollector"
```

### 7.3 Database Collector paketi

```powershell
dotnet publish .\src\Inventory.SqlCollector\Inventory.SqlCollector.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  --no-restore `
  -o "$PackageRoot\DatabaseCollector"
```

Runtime-specific restore gerektiğinde önce aşağıdaki komutları çalıştırın:

```powershell
dotnet restore .\src\Inventory.IisCollector\Inventory.IisCollector.csproj -r win-x64 --configfile .\NuGet.Config
dotnet restore .\src\Inventory.SqlCollector\Inventory.SqlCollector.csproj -r win-x64 --configfile .\NuGet.Config
```

## 8. Ortama özel appsettings dosyaları

Gerçek connection string ve grup adlarını source control'e yazmayın. Publish sonrasında staging paketlerindeki dosyaları ortam değerleriyle düzenleyin.

### 8.1 Web `appsettings.json`

`Web\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "InventoryDatabase": "Server=mydb01.ae.local;Database=PSMEnvanter;Integrated Security=True;Encrypt=True;TrustServerCertificate=False",
    "TestConnectionsDatabase": "Server=mydb01.ae.local;Database=IISEnvanter;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;Pooling=True;Min Pool Size=0;Max Pool Size=50;Application Name=PSMCollector.TestConnections"
  },
  "Authorization": {
    "AdminGroups": [ "AE\\PSMCollector-Admins" ],
    "OperatorGroups": [ "AE\\PSMCollector-Operators" ],
    "ReaderGroups": [ "AE\\PSMCollector-Readers" ]
  }
}
```

`TestConnectionsDatabase` yalnız test sayfası kullanılacaksa gereklidir. Üretim ortamında test sayfası kullanılmayacaksa ilgili erişimi vermeyin ve sayfayı yetkilendirme politikasıyla sınırlandırın.

### 8.2 IIS Collector `appsettings.json`

`IisCollector\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "InventoryDatabase": "Server=mydb01.ae.local;Database=PSMEnvanter;Integrated Security=True;Encrypt=True;TrustServerCertificate=False"
  },
  "Collector": {
    "PollIntervalSeconds": 5,
    "CollectionIntervalSeconds": 60,
    "HeartbeatIntervalSeconds": 15,
    "PowerShellPath": "powershell.exe"
  }
}
```

`CollectionIntervalSeconds` tüm etkin IIS hedefleri için collection aralığıdır. Değer saniye cinsindedir. Değişiklikten sonra `PSMCollector.IisCollector` servisi yeniden başlatılmalıdır.

### 8.3 Database Collector `appsettings.json`

`DatabaseCollector\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "InventoryDatabase": "Server=mydb01.ae.local;Database=PSMEnvanter;Integrated Security=True;Encrypt=True;TrustServerCertificate=False"
  },
  "Collector": {
    "PollIntervalSeconds": 2,
    "HeartbeatIntervalSeconds": 15,
    "MaxSnapshotAgeSeconds": 120
  }
}
```

Database Collector bağımsız bir collection başlatmaz. IIS Collector tarafından hazırlanan snapshot'ları işler. `PollIntervalSeconds`, hazır işlerin kontrol sıklığıdır.

## 9. Database scriptlerinin pakete eklenmesi

Repository'deki şema scriptini kopyalayın:

```powershell
Copy-Item .\database\PSMEnvanter.sql "$PackageRoot\Database\PSMEnvanter.sql"
Copy-Item .\HOWTO_DEPLOY.md "$PackageRoot\HOWTO_DEPLOY.md"
```

Merkezi izin scripti olarak aşağıdaki içeriği `Permissions-Central.sql` adıyla pakete koyun. Domain ve hesap adlarını ortama göre değiştirin:

```sql
USE [master];
GO
IF SUSER_ID(N'AE\gmsaSPWorker$') IS NULL
    CREATE LOGIN [AE\gmsaSPWorker$] FROM WINDOWS;
IF SUSER_ID(N'AE\gmsaSPWeb$') IS NULL
    CREATE LOGIN [AE\gmsaSPWeb$] FROM WINDOWS;
GO

USE [PSMEnvanter];
GO
IF USER_ID(N'AE\gmsaSPWorker$') IS NULL
    CREATE USER [AE\gmsaSPWorker$] FOR LOGIN [AE\gmsaSPWorker$];
IF USER_ID(N'AE\gmsaSPWeb$') IS NULL
    CREATE USER [AE\gmsaSPWeb$] FOR LOGIN [AE\gmsaSPWeb$];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members drm
    WHERE drm.role_principal_id = DATABASE_PRINCIPAL_ID(N'db_datareader')
      AND drm.member_principal_id = DATABASE_PRINCIPAL_ID(N'AE\gmsaSPWorker$'))
    ALTER ROLE [db_datareader] ADD MEMBER [AE\gmsaSPWorker$];
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members drm
    WHERE drm.role_principal_id = DATABASE_PRINCIPAL_ID(N'db_datawriter')
      AND drm.member_principal_id = DATABASE_PRINCIPAL_ID(N'AE\gmsaSPWorker$'))
    ALTER ROLE [db_datawriter] ADD MEMBER [AE\gmsaSPWorker$];
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members drm
    WHERE drm.role_principal_id = DATABASE_PRINCIPAL_ID(N'db_datareader')
      AND drm.member_principal_id = DATABASE_PRINCIPAL_ID(N'AE\gmsaSPWeb$'))
    ALTER ROLE [db_datareader] ADD MEMBER [AE\gmsaSPWeb$];
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members drm
    WHERE drm.role_principal_id = DATABASE_PRINCIPAL_ID(N'db_datawriter')
      AND drm.member_principal_id = DATABASE_PRINCIPAL_ID(N'AE\gmsaSPWeb$'))
    ALTER ROLE [db_datawriter] ADD MEMBER [AE\gmsaSPWeb$];
GO
```

İzlenecek hedef SQL Server izin scripti `Permissions-TargetSql.sql`:

```sql
USE [master];
GO
IF SUSER_ID(N'AE\gmsaSPWeb$') IS NULL
    CREATE LOGIN [AE\gmsaSPWeb$] FROM WINDOWS;
GO
GRANT VIEW SERVER PERFORMANCE STATE TO [AE\gmsaSPWeb$];
GO
```

SQL Server 2022 için öncelikli minimum DMV izni `VIEW SERVER PERFORMANCE STATE` iznidir. Collector hesabına `sysadmin` vermeyin.

TestConnections sayfası kullanılacaksa ayrıca:

```sql
USE [IISEnvanter];
GO
IF USER_ID(N'AE\gmsaSPWeb$') IS NULL
    CREATE USER [AE\gmsaSPWeb$] FOR LOGIN [AE\gmsaSPWeb$];
GO
GRANT SELECT ON OBJECT::dbo.IisInventoryRun TO [AE\gmsaSPWeb$];
GO
```

## 10. Paket checksum'larının oluşturulması

```powershell
New-Item -ItemType Directory -Path "$PackageRoot\Checksums" -Force
Get-ChildItem $PackageRoot -File -Recurse |
  Where-Object FullName -NotMatch '\\Checksums\\' |
  Get-FileHash -Algorithm SHA256 |
  ForEach-Object { "{0} *{1}" -f $_.Hash, $_.Path.Substring($PackageRoot.Length + 1) } |
  Set-Content "$PackageRoot\Checksums\SHA256SUMS.txt"
```

File share'e kopyaladıktan sonra hedef sunucuda kritik paketlerin hash'lerini yeniden karşılaştırın.

## 11. Paketlerin file share'e aktarılması

```powershell
$ShareTarget = '\\fileserver\software\PSMCollector\1.0.0'
New-Item -ItemType Directory -Path $ShareTarget -Force
Copy-Item "$PackageRoot\*" $ShareTarget -Recurse -Force
```

Build sorumlusu bu noktadan sonra sunucuda işlem yapmaz. Database ve Windows yöneticilerine:

- paket yolu,
- sürüm,
- Git commit kimliği,
- SHA256 dosyası,
- bakım penceresi

iletilir.

## 12. AD ve gMSA önkoşulları

Bu işlemleri Domain/Windows yöneticisi yapmalıdır.

Örnek hesaplar:

- IIS Collector: `AE\gmsaSPWorker$`
- Web ve Database Collector: `AE\gmsaSPWeb$`

Gereksinimler:

- Uygulama sunucusunun iki gMSA parolasını alma yetkisi olmalıdır.
- İki gMSA uygulama sunucusunda kurulmuş olmalıdır.
- Windows Service hesaplarına `Log on as a service` hakkı verilmelidir.
- Web gMSA, IIS app pool identity olarak kullanılabilmelidir.
- IIS Collector gMSA hedef IIS sunucularında IIS metadata, worker process ve TCP bağlantılarını okuyabilmelidir.
- Parola hiçbir script veya configuration dosyasına yazılmamalıdır.

Uygulama sunucusunda yönetici PowerShell:

```powershell
Import-Module ActiveDirectory
Install-ADServiceAccount -Identity gmsaSPWorker
Install-ADServiceAccount -Identity gmsaSPWeb
Test-ADServiceAccount -Identity gmsaSPWorker
Test-ADServiceAccount -Identity gmsaSPWeb
```

Her iki test de `True` dönmelidir.

gMSA kurulumu merkezi GPO/AD ekibi tarafından yönetiliyorsa yerel komutları kurum standardınıza göre uyarlayın.

## 13. DNS, sertifika ve SPN

DNS yöneticisi aşağıdaki kaydı oluşturur:

```text
psmcollector.ae.local -> uygulama sunucusu IP adresi
```

Web sertifikasının Subject veya SAN alanında `psmcollector.ae.local` bulunmalıdır. Sertifikayı `Local Computer\Personal` store'a private key ile import edin.

Domain yöneticisi HTTP SPN'lerini web gMSA üzerine kaydeder:

```powershell
setspn -S HTTP/psmcollector AE\gmsaSPWeb$
setspn -S HTTP/psmcollector.ae.local AE\gmsaSPWeb$
setspn -Q HTTP/psmcollector
setspn -Q HTTP/psmcollector.ae.local
```

Bir SPN yalnız tek AD nesnesinde bulunmalıdır. Duplicate SPN varsa kuruluma devam etmeyin.

## 14. Merkezi veritabanının kurulması

Database yöneticisi paketi file share'den SQL Server üzerindeki yerel staging dizinine kopyalar:

```powershell
New-Item -ItemType Directory -Path C:\Install\PSMCollector\Database -Force
Copy-Item '\\fileserver\software\PSMCollector\1.0.0\Database\*' C:\Install\PSMCollector\Database -Force
```

SQL Server'da Windows Authentication ile:

```powershell
sqlcmd -S localhost -E -d master -b -Q "IF DB_ID(N'PSMEnvanter') IS NULL CREATE DATABASE [PSMEnvanter];"
sqlcmd -S localhost -E -d PSMEnvanter -b -i C:\Install\PSMCollector\Database\PSMEnvanter.sql
sqlcmd -S localhost -E -d master -b -i C:\Install\PSMCollector\Database\Permissions-Central.sql
```

Named instance veya farklı port kullanılıyorsa `-S` parametresini uyarlayın. Uygulama kapsamı yalnız standart SQL TCP `1433` hedeflerini destekler.

Doğrulama:

```sql
USE PSMEnvanter;
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
SELECT COUNT(*) AS IisServerCount FROM dbo.IisServers;
SELECT COUNT(*) AS SqlServerCount FROM dbo.SqlServers;
```

## 15. Hedef SQL Server izinleri

İzlenecek her SQL Server üzerinde, o sunucunun DBA'sı yerel olarak:

```powershell
sqlcmd -S localhost -E -d master -b -i C:\Install\PSMCollector\Database\Permissions-TargetSql.sql
```

çalıştırır.

Hedef SQL Server'da kalıcı PSMCollector tablosu, procedure, view veya job oluşturulmaz. Collector yalnız açık bağlantı süresince yaşayan `#IisConnections` local temporary table'ını kullanır.

## 16. Uygulama sunucusu önkoşulları

Windows yöneticisi aşağıdakileri doğrular:

- Windows Server 2022 veya desteklenen sürüm
- IIS Web Server
- Windows Authentication IIS role service
- PowerShell ve `WebAdministration` modülü
- `Get-NetTCPConnection` cmdlet'i
- ASP.NET Core Hosting Bundle 10
- SQL Server `1433` portuna ağ erişimi
- Merkezi DB'ye gMSA kimlikleriyle erişim
- Hedef IIS sunucularına WinRM/Kerberos erişimi

Hosting Bundle offline kurulumu:

```powershell
Start-Process C:\Install\Prerequisites\dotnet-hosting-10.x-win.exe -ArgumentList '/quiet','/norestart' -Wait
iisreset
```

Kurulum dosyasının adı gerçek paket adına göre değiştirilmelidir.

## 17. Paketlerin uygulama sunucusuna kopyalanması

Sunucuda yerel yönetici PowerShell:

```powershell
$Source = '\\fileserver\software\PSMCollector\1.0.0'
New-Item -ItemType Directory -Path C:\Apps\PSMCollector\IisCollector -Force
New-Item -ItemType Directory -Path C:\Apps\PSMCollector\DatabaseCollector -Force
New-Item -ItemType Directory -Path C:\inetpub\PSMCollector -Force

Copy-Item "$Source\IisCollector\*" C:\Apps\PSMCollector\IisCollector -Recurse -Force
Copy-Item "$Source\DatabaseCollector\*" C:\Apps\PSMCollector\DatabaseCollector -Recurse -Force
Copy-Item "$Source\Web\*" C:\inetpub\PSMCollector -Recurse -Force
```

Kopyalamadan sonra `appsettings.json` içindeki gerçek ortam değerlerini tekrar kontrol edin.

## 18. Windows Service kurulumu

gMSA parolası boş/geçersiz parola gibi ele alınmamalıdır. Aşağıdaki WMI tabanlı fonksiyon gMSA için parola saklamadan servis oluşturur.

Uygulama sunucusunda yönetici PowerShell:

```powershell
function New-GmsaService {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $DisplayName,
        [Parameter(Mandatory)] [string] $ExecutablePath,
        [Parameter(Mandatory)] [string] $Account
    )

    if (Get-Service -Name $Name -ErrorAction SilentlyContinue) {
        throw "Service already exists: $Name"
    }

    $serviceClass = [wmiclass]'Win32_Service'
    $quotedPath = '"' + $ExecutablePath + '"'
    $result = $serviceClass.Create(
        $Name,
        $DisplayName,
        $quotedPath,
        16,
        1,
        'Automatic',
        $false,
        $Account,
        $null,
        $null,
        $null,
        $null)

    if ($result.ReturnValue -ne 0) {
        throw "Service creation failed. Win32 code: $($result.ReturnValue)"
    }
}

New-GmsaService `
  -Name 'PSMCollector.IisCollector' `
  -DisplayName 'PSMCollector IIS Collector' `
  -ExecutablePath 'C:\Apps\PSMCollector\IisCollector\Inventory.IisCollector.exe' `
  -Account 'AE\gmsaSPWorker$'

New-GmsaService `
  -Name 'PSMCollector.DatabaseCollector' `
  -DisplayName 'PSMCollector Database Collector' `
  -ExecutablePath 'C:\Apps\PSMCollector\DatabaseCollector\Inventory.SqlCollector.exe' `
  -Account 'AE\gmsaSPWeb$'

Start-Service PSMCollector.IisCollector
Start-Service PSMCollector.DatabaseCollector
```

Doğrulama:

```powershell
Get-CimInstance Win32_Service -Filter "Name='PSMCollector.IisCollector' OR Name='PSMCollector.DatabaseCollector'" |
  Select-Object Name, State, StartMode, StartName, PathName
```

Beklenen:

```text
PSMCollector.IisCollector       Running Auto AE\gmsaSPWorker$
PSMCollector.DatabaseCollector  Running Auto AE\gmsaSPWeb$
```

Servis başlamazsa Application Event Log'u kontrol edin:

```powershell
Get-WinEvent -FilterHashtable @{ LogName='Application'; StartTime=(Get-Date).AddMinutes(-15) } |
  Where-Object ProviderName -Match 'Inventory|PSMCollector|.NET Runtime' |
  Select-Object TimeCreated, LevelDisplayName, ProviderName, Id, Message
```

## 19. IIS web sitesinin kurulması

Uygulama sunucusunda yönetici PowerShell:

```powershell
Import-Module WebAdministration

New-WebAppPool -Name 'PSMCollector'
Set-ItemProperty 'IIS:\AppPools\PSMCollector' -Name managedRuntimeVersion -Value ''
Set-ItemProperty 'IIS:\AppPools\PSMCollector' -Name processModel.identityType -Value 3
Set-ItemProperty 'IIS:\AppPools\PSMCollector' -Name processModel.userName -Value 'AE\gmsaSPWeb$'
Set-ItemProperty 'IIS:\AppPools\PSMCollector' -Name processModel.password -Value ''
Set-ItemProperty 'IIS:\AppPools\PSMCollector' -Name processModel.loadUserProfile -Value $true

New-Website `
  -Name 'PSMCollector' `
  -PhysicalPath 'C:\inetpub\PSMCollector' `
  -ApplicationPool 'PSMCollector' `
  -Port 8090 `
  -HostHeader 'psmcollector.ae.local'
```

HTTPS binding için sertifika thumbprint'ini bulun:

```powershell
$certificate = Get-ChildItem Cert:\LocalMachine\My |
  Where-Object { $_.Subject -like '*psmcollector.ae.local*' -or $_.DnsNameList.Unicode -contains 'psmcollector.ae.local' } |
  Sort-Object NotAfter -Descending |
  Select-Object -First 1

if (-not $certificate) { throw 'PSMCollector certificate was not found.' }

New-WebBinding -Name 'PSMCollector' -Protocol https -Port 8080 -HostHeader 'psmcollector.ae.local' -SslFlags 1
Get-Item "Cert:\LocalMachine\My\$($certificate.Thumbprint)" |
  New-Item 'IIS:\SslBindings\0.0.0.0!8080!psmcollector.ae.local'
```

Ortamınız SNI kullanmıyorsa binding ve SSL binding komutlarını kurum standardınıza göre değiştirin.

## 20. Windows Authentication ayarları

```powershell
Import-Module WebAdministration

Set-WebConfigurationProperty `
  -PSPath 'MACHINE/WEBROOT/APPHOST' `
  -Location 'PSMCollector' `
  -Filter 'system.webServer/security/authentication/anonymousAuthentication' `
  -Name enabled `
  -Value $false

Set-WebConfigurationProperty `
  -PSPath 'MACHINE/WEBROOT/APPHOST' `
  -Location 'PSMCollector' `
  -Filter 'system.webServer/security/authentication/windowsAuthentication' `
  -Name enabled `
  -Value $true

Set-WebConfigurationProperty `
  -PSPath 'MACHINE/WEBROOT/APPHOST' `
  -Location 'PSMCollector' `
  -Filter 'system.webServer/security/authentication/windowsAuthentication' `
  -Name useKernelMode `
  -Value $true

Set-WebConfigurationProperty `
  -PSPath 'MACHINE/WEBROOT/APPHOST' `
  -Location 'PSMCollector' `
  -Filter 'system.webServer/security/authentication/windowsAuthentication' `
  -Name useAppPoolCredentials `
  -Value $true

Restart-WebAppPool -Name 'PSMCollector'
Start-Website -Name 'PSMCollector'
```

Kontrol:

```powershell
Get-Website -Name PSMCollector
Get-WebBinding -Name PSMCollector
Get-WebAppPoolState -Name PSMCollector
```

## 21. Firewall

Uygulama sunucusunda inbound web portları için kurum onaylı firewall kuralı oluşturun:

- TCP 8080: HTTPS
- TCP 8090: yalnız HTTP redirect/test gerekiyorsa

Uygulama sunucusundan outbound:

- Merkezi ve hedef SQL Server'lara TCP 1433
- Hedef IIS sunucularına WinRM/Kerberos için kurumun gerekli gördüğü portlar
- Domain Controller/DNS/Kerberos erişimi

Firewall kurallarını mümkün olduğunca kaynak subnet ve yönetim ağı ile sınırlandırın.

## 22. İlk hedef kayıtlarının eklenmesi

Web UI üzerinden Admin rolüyle:

1. IIS Sunucuları ekranında hedef IIS sunucusunu ekleyin.
2. FQDN, collection interval ve timeout değerlerini kontrol edin.
3. SQL Sunucuları ekranında SQL Server adını, FQDN'ini ve IIS sunucusundan görünen IPv4 adresini ekleyin.
4. Port `1433` olmalıdır.
5. Sertifika güveni doğru yapılandırılmışsa `TrustServerCertificate` kapalı kalmalıdır.

UI henüz kullanılamıyorsa ilk kayıtlar DBA tarafından kontrollü SQL scriptiyle eklenebilir; audit alanları ve constraint'ler mutlaka doldurulmalıdır. Doğrudan SQL ekleme yalnız ilk bootstrap için kullanılmalıdır.

## 23. Kurulum doğrulaması

### 23.1 Servisler

```powershell
Get-Service PSMCollector.IisCollector,PSMCollector.DatabaseCollector
```

İki servis de `Running` olmalıdır.

### 23.2 Web

Domain'e bağlı bir istemciden:

```powershell
curl.exe -k -I --negotiate -u : https://psmcollector.ae.local:8080/
```

Geçerli sertifika kurulduktan sonra `-k` kullanmayın. Beklenen sonuç `HTTP 200` veya uygulamanın yönlendirme yanıtıdır.

### 23.3 Collector heartbeat

DBA merkezi veritabanında:

```sql
USE PSMEnvanter;
SELECT
    CollectorType,
    InstanceName,
    LastHeartbeatUtc,
    DATEDIFF(SECOND, LastHeartbeatUtc, SYSUTCDATETIME()) AS AgeSeconds,
    Version,
    StatusMessage
FROM dbo.CollectorHeartbeats
ORDER BY CollectorType, InstanceName;
```

Heartbeat yaşı normalde yapılandırılmış heartbeat süresinin birkaç katını aşmamalıdır.

### 23.4 Collection run

```sql
SELECT TOP (10)
    Id,
    StartedAtUtc,
    CompletedAtUtc,
    Status,
    IisServerCount,
    SuccessfulIisServerCount,
    FailedIisServerCount,
    StagedConnectionCount,
    MatchedConnectionCount,
    UnmatchedConnectionCount,
    ErrorSummary
FROM dbo.CollectionRuns
ORDER BY StartedAtUtc DESC;
```

Durum değerleri:

| Değer | Durum |
|---:|---|
| 0 | PendingIis |
| 1 | ReadyForSql |
| 2 | ProcessingSql |
| 3 | Completed |
| 4 | Partial |
| 5 | Failed |
| 6 | Expired |

### 23.5 Envanter

```sql
SELECT TOP (100)
    InventoryDateUtc,
    IisServerName,
    AppPoolName,
    SqlServerName,
    DatabaseName,
    TotalConnections,
    ActiveConnections,
    IdlePooledConnections
FROM dbo.ConnectionInventory
ORDER BY InventoryDateUtc DESC;
```

Her satırda:

```text
TotalConnections = ActiveConnections + IdlePooledConnections
```

olmalıdır.

## 24. TestConnections sayfası

Test sayfası:

```text
https://psmcollector.ae.local:8080/test-connections
```

Sayfa açıldığında örnek `IISEnvanter` veritabanına 50 bağlantı açar. Butonlar:

- `50 Bağlantıyı Aç`
- `40 Bağlantıyı Aktif Yap`
- `Aktif Sorguları Durdur`
- `Bağlantıları Kapat`
- `Verileri Yenile`

Aktif test, 40 bağlantıda iptal edilebilir uzun süreli `WAITFOR` request çalıştırır; kalan 10 bağlantı idle kalır. Bu sayfa yalnız test içindir.

Üretim güvenliği için:

- Sayfayı yalnız yetkili test kullanıcılarına açın.
- Test bittiğinde aktif sorguları durdurun ve bağlantıları kapatın.
- Gerek yoksa `IISEnvanter` SELECT iznini kaldırın.
- Uzun süre açık test connection'larını normal üretim yüküyle karıştırmayın.

DMV doğrulaması:

```sql
SELECT
    DB_NAME(s.database_id) AS DatabaseName,
    s.program_name,
    s.login_name,
    COUNT(*) AS OpenSessions,
    SUM(CASE WHEN r.session_id IS NULL THEN 0 ELSE 1 END) AS ActiveSessions,
    SUM(CASE WHEN r.session_id IS NULL THEN 1 ELSE 0 END) AS IdleSessions
FROM sys.dm_exec_sessions s
LEFT JOIN sys.dm_exec_requests r ON r.session_id = s.session_id
WHERE s.program_name = N'PSMCollector.TestConnections'
GROUP BY s.database_id, s.program_name, s.login_name;
```

## 25. Collection interval değiştirme

Yalnız IIS Collector dosyasını değiştirin:

```text
C:\Apps\PSMCollector\IisCollector\appsettings.json
```

Örnek:

```json
"CollectionIntervalSeconds": 60
```

Ardından:

```powershell
Restart-Service PSMCollector.IisCollector
```

Database Collector'daki `PollIntervalSeconds`, collection aralığı değil hazır iş kontrol sıklığıdır.

## 26. Offline upgrade

Yeni sürüm için build sorumlusu aynı paketleme adımlarını yeni sürüm klasöründe tekrarlar. Uygulama sunucusu yöneticisi bakım penceresinde:

```powershell
$Version = '1.1.0'
$Source = "\\fileserver\software\PSMCollector\$Version"
$BackupRoot = "C:\Apps\PSMCollector_Backup\$(Get-Date -Format yyyyMMdd_HHmmss)"

Stop-Service PSMCollector.IisCollector
Stop-Service PSMCollector.DatabaseCollector
Import-Module WebAdministration
Stop-Website PSMCollector
Stop-WebAppPool PSMCollector

New-Item -ItemType Directory -Path $BackupRoot -Force
Copy-Item C:\Apps\PSMCollector "$BackupRoot\Collectors" -Recurse
Copy-Item C:\inetpub\PSMCollector "$BackupRoot\Web" -Recurse

Copy-Item "$Source\IisCollector\*" C:\Apps\PSMCollector\IisCollector -Recurse -Force
Copy-Item "$Source\DatabaseCollector\*" C:\Apps\PSMCollector\DatabaseCollector -Recurse -Force
Copy-Item "$Source\Web\*" C:\inetpub\PSMCollector -Recurse -Force

Start-WebAppPool PSMCollector
Start-Website PSMCollector
Start-Service PSMCollector.IisCollector
Start-Service PSMCollector.DatabaseCollector
```

Önceki ortama özel `appsettings.json` dosyalarını yeni paketle ezmeden önce karşılaştırın. Yeni sürümde eklenen configuration alanlarını merge edin.

Database migration varsa uygulama binary'lerinden önce DBA tarafından uygulanmalı ve geri dönüş planı hazırlanmalıdır.

## 27. Rollback

Rollback kararı verilirse:

1. Web sitesi ve iki collector servisi durdurulur.
2. Başarısız sürüm dizinleri silinmeden ayrı bir inceleme klasörüne taşınır.
3. Backup paketleri orijinal dizinlere geri kopyalanır.
4. Gerekliyse DBA onaylı database rollback uygulanır.
5. Web sitesi ve servisler başlatılır.
6. Heartbeat ve son collection doğrulanır.

Database migration rollback'i binary rollback'ten ayrı değerlendirilmelidir. Veri kaybına neden olabilecek rollback scriptleri yalnız DBA onayıyla çalıştırılmalıdır.

## 28. Sık karşılaşılan sorunlar

### Servis 1069 ile başlamıyor

- gMSA uygulama sunucusunda kurulu olmayabilir.
- Sunucunun managed password alma yetkisi olmayabilir.
- `Log on as a service` hakkı eksik olabilir.
- Servis hesabı sonunda `$` bulunmayabilir.

Kontrol:

```powershell
Test-ADServiceAccount gmsaSPWorker
Test-ADServiceAccount gmsaSPWeb
```

### Merkezi DB login hatası

- SQL login/user oluşturulmamış olabilir.
- gMSA merkezi DB rollerinde olmayabilir.
- SQL TLS sertifikası FQDN ile eşleşmiyor olabilir.
- Connection string IP ile değil doğru FQDN ile kullanılmalıdır.

### Web 401 döndürüyor

- SPN yanlış nesnede veya duplicate olabilir.
- App pool identity yanlış olabilir.
- `useKernelMode=True` ve `useAppPoolCredentials=True` olmayabilir.
- Anonymous Authentication açık olabilir.
- İstemci DNS adı yerine IP ile erişiyor olabilir.

### IIS Collector WinRM/Kerberos hatası

- Hedef FQDN/SPN/DNS uyuşmuyor olabilir.
- IIS Collector gMSA hedefte remote management yetkisine sahip olmayabilir.
- WinRM listener/firewall kapalı olabilir.
- Domain trust veya zaman senkronizasyonu sorunu olabilir.

### Run tamamlanıyor fakat envanter sıfır

- Snapshot anında ilgili app pool'un açık 1433 socket'i olmayabilir.
- SQL Server IP kaydı `SqlServers.IpAddress` ile IIS'in gördüğü remote IP uyuşmuyor olabilir.
- Hedef SQL login/DMV izni eksik olabilir.
- Connection, collector snapshot'ından önce kapanmış olabilir.

### Run `Expired`

- IIS snapshot ile SQL işleme arasındaki süre `MaxSnapshotAgeSeconds` değerini aşmıştır.
- Database Collector durmuş veya DB erişimi kesilmiş olabilir.

## 29. Kurulum teslim kontrol listesi

- [ ] Dağıtılan Git commit ve sürüm kaydedildi.
- [ ] Build/test başarılı.
- [ ] Offline paket checksum'ları doğrulandı.
- [ ] Merkezi DB ve migration'lar uygulandı.
- [ ] gMSA login/user/izinleri uygulandı.
- [ ] Hedef SQL DMV izinleri uygulandı.
- [ ] İki gMSA uygulama sunucusunda doğrulandı.
- [ ] IIS Collector doğru gMSA ile Running/Automatic.
- [ ] Database Collector doğru gMSA ile Running/Automatic.
- [ ] IIS sitesi ve app pool Started.
- [ ] DNS, sertifika ve SPN doğrulandı.
- [ ] Windows Authentication ile web `200 OK`.
- [ ] Collector heartbeat'leri güncel.
- [ ] En az bir collection `Completed`.
- [ ] Envanter toplam kontrolü doğru.
- [ ] Backup ve rollback dizini belirlendi.
- [ ] TestConnections kullanıldıysa test yükü kapatıldı.

## 30. Güvenlik notları

- Parolaları veya private key'leri file share paketine koymayın.
- gMSA parolası hiçbir zaman elle girilmez veya saklanmaz.
- File share erişimini build ve kurulum ekipleriyle sınırlandırın.
- Paket hash'lerini doğrulayın.
- SQL collector'a `sysadmin` vermeyin.
- IIS collector'a gereksiz Domain Admin veya geniş local admin yetkisi vermeyin.
- `TrustServerCertificate=True` yalnız kontrollü geçici testte kullanılmalıdır.
- Hedef SQL Server'larda kalıcı PSMCollector nesnesi oluşturmayın.
- TestConnections sayfasını test bitince kapatın veya erişimini sınırlandırın.
