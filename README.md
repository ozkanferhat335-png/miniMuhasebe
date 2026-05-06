# MiniMuhasebe Pro (WinForms)

Banka API entegre muhasebe uygulaması için modüler başlangıç projesi.

## Katmanlar
- Core: uygulama ayarları
- Data: veritabanı başlatma/sürdürme
- UI: WinForms ekranları
- Services/Banking: iş servisleri ve banka adaptörleri için ayrılmış klasörler

## Veritabanı
Uygulama açılırken `DatabaseInitializer` sınıfı otomatik olarak tabloları oluşturur.
Ayrı SQL script kullanılmaz.

`App.config` ile sağlayıcı seçimi:
- SQLite (varsayılan)
- SQLServer

## Not
Bu sürüm, SRS'teki modüler ekran yapısı ve çekirdek şema gereksinimlerini karşılayan v1 temel iskelettir.
