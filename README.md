# MiniMuhasebe Pro (WinForms)

Bu proje, SRS gereksinimlerine göre hazırlanmış modüler Windows Forms muhasebe uygulamasıdır.

## Öne Çıkanlar
- Rol bazlı kimlik doğrulama altyapısı (Admin, Muhasebe, Finans, İzleyici)
- Banka adaptör mimarisi (`IBankAdapter`) ile genişletilebilir entegrasyon
- EFT/Havale, mutabakat, raporlama, muhasebe fişleri için modül ekranları
- Audit log altyapısı ve IBAN maskeleme yardımcıları
- Veritabanı tablolarının uygulama kodu içinde otomatik oluşturulması (ayrı script yok)

## Teknoloji
- .NET Framework 4.8
- C# 7.3
- WinForms
- SQLite veya SQL Server

## Veritabanı Yaklaşımı
`Data/DatabaseInitializer.cs` uygulama açılışında çalışır ve gerekli tabloları oluşturur.
Sağlayıcı seçimi `App.config` içindeki `DatabaseProvider` değeri ile yapılır:
- `SQLite` (varsayılan)
- `SQLServer`
