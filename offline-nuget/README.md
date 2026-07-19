# PSMCollector Offline NuGet Feed

Bu dizin, solution'ın restore/build/test işlemleri ile `win-x64` self-contained collector publish işlemleri için gereken sürüm kilitli `.nupkg` dosyalarını içerir.

Kullanım:

```powershell
.\scripts\Build-Offline.ps1
```

Manuel restore:

```powershell
dotnet restore .\IisSqlConnectionInventory.slnx --configfile .\NuGet.Offline.Config
```

Paket eklerken veya sürüm yükseltirken yalnız resmi/güvenilir kaynaktan alınan paketleri kullanın, lisans ve güvenlik taramasını kurum politikasına göre tekrarlayın ve temiz global cache ile offline restore doğrulaması yapın.
