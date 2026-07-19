# Geliştirme Durumu

## Aşama 1 — Solution, domain ve veri modeli

Durum: Tamamlandı

- `.NET 10` solution ve zorunlu projeler oluşturuldu.
- IIS/SQL sunucuları, collection run, staging, kalıcı envanter, bilinmeyen endpoint ve audit entity'leri eklendi.
- SQL Server şeması; `rowversion`, foreign key, unique index ve check constraint'lerle tanımlandı.
- `InitialInventorySchema` migration'ı üretildi.
- Domain zamanlama ve bağlantı toplamı kuralları test edildi.

## Aşama 2 — Merkezi DB erişimi ve koordinasyon

Durum: Devam ediyor

- Repository ve unit-of-work sınırları
- Atomik run/lease işlemleri
- Staging bulk write ve unknown endpoint upsert
- Collector command queue ve service heartbeat modeli

Eklenenler: collector command/heartbeat şeması, EF tabanlı command store, run coordinator, idempotent inventory writer ve socket/session aggregation çekirdeği.

## Üretim dağıtım durumu

- `mydb01.ae.local/PSMEnvanter` oluşturuldu ve iki EF migration uygulandı.
- `myapp01` ve `mydb01` başlangıç hedef kayıtları eklendi.
- `myapp01.ae.local` üzerinde `PSMCollector` sitesi ve app pool oluşturuldu.
- HTTPS binding: `https://psmcollector.ae.local:8080`.
- Windows Authentication doğrulandı: `HTTP/psmcollector` ve `HTTP/psmcollector.ae.local` SPN'leri `AE\gmsaSPWeb$` hesabında; IIS kernel-mode authentication app pool credentials kullanıyor ve HTTPS isteği `200 OK` dönüyor.
- `PSMCollector.IisCollector` Windows Service'i `AE\gmsaSPWorker$` ile çalışıyor.
- `PSMCollector.DatabaseCollector` Windows Service'i `AE\gmsaSPWeb$` ile çalışıyor.
- IIS collection varsayılan olarak 60 saniyede bir çalışıyor; `IisCollector/appsettings.json` içindeki `Collector:CollectionIntervalSeconds` ile parametrik.
- Collector heartbeat, IIS app pool/PID/TCP 1433 snapshot, SQL DMV eşleştirme ve kalıcı envanter yazımı üretimde uçtan uca doğrulandı.

## Sonraki aşamalar

1. Fake IIS/SQL provider'larla orchestration
2. Özetleme, idempotency, expiry ve retention
3. Blazor yönetim ve envanter ekranları
4. Gerçek IIS ve SQL adapter'ları
5. Windows Authentication, authorization ve audit
6. Windows Service packaging ve deployment araçları
