# IIS–SQL Connection Inventory

Merkezi bir yönetim sunucusundan IIS Application Pool bağlantılarını SQL Server oturumlarıyla ilişkilendiren .NET 10 çözümüdür.

## Mevcut durum

İlk geliştirme aşaması tamamlandı:

- Dokuz projeli solution iskeleti
- Temel domain entity ve enum'ları
- SQL Server için EF Core `InventoryDbContext`
- Veri bütünlüğü constraint ve indeksleri
- İlk EF Core migration
- Temel domain testleri

Ayrıntılı gereksinimler için [CODEX_HANDOFF.md](CODEX_HANDOFF.md), aşama durumu için [DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) dosyasına bakın.

## Yerel doğrulama

```powershell
dotnet restore .\IisSqlConnectionInventory.slnx --configfile .\NuGet.Config
dotnet build .\IisSqlConnectionInventory.slnx --no-restore
dotnet test .\IisSqlConnectionInventory.slnx --no-build --no-restore
```

İnternetsiz restore için repository içindeki yerel NuGet feed kullanılabilir:

```powershell
dotnet restore .\IisSqlConnectionInventory.slnx --configfile .\NuGet.Offline.Config
```

Self-contained Windows collector paketleri için runtime restore:

```powershell
dotnet restore .\src\Inventory.IisCollector\Inventory.IisCollector.csproj -r win-x64 --configfile .\NuGet.Offline.Config
dotnet restore .\src\Inventory.SqlCollector\Inventory.SqlCollector.csproj -r win-x64 --configfile .\NuGet.Offline.Config
```

Gerçek bağlantı dizeleri ve ortam hesapları repository'ye eklenmemelidir.
