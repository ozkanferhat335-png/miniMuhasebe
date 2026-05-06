# Kayıp Renkler Diyarı — Kapsamlı Gereksinim Dokümanı (v2)

> Hedef: Bu doküman, oyunun uygulanabilirliğini artırmak için önceki sürüme göre kapsamı yaklaşık 5 kat genişletilmiş; teknik, tasarımsal, oynanışsal ve kalite odaklı ayrıntıları netleştirilmiş bir gereksinim setidir.

---

## 1. Vizyon, Amaç ve Kapsam

### 1.1 Oyun Vizyonu
Kayıp Renkler Diyarı, renklerin karanlık bir varlık tarafından çalındığı bir dünyada geçen, hikâye odaklı 2D bulmaca-platform oyunudur. Oyuncu, stickman kahramanı yöneterek her bölümde kayıp renk parçalarını geri toplar, çevresel bulmacaları çözer, engelleri aşar ve finalde renkleri dünyaya geri kazandırır.

### 1.2 Proje Amaçları
- 10 bölümlük tam bir oynanış döngüsü sunmak.
- Her bölümde en az 1 yeni mekanik öğreterek öğrenme eğrisi oluşturmak.
- Düşük sistem gereksinimlerinde akıcı performans sağlamak.
- Tek oyunculu, klavye kontrollü, kısa-orta oturumlu (5–15 dk/bölüm) bir deneyim sunmak.

### 1.3 Kapsam Dahili
- Menü, ayarlar, kayıt sistemi, bölüm kilit açma sistemi
- 10 bölüm harita ve bulmaca akışı
- Stickman karakter animasyon/poz çizimi
- Çarpışma, fizik, hasar, can, yeniden doğma sistemi
- Toplanabilir renk parçaları ve kapı açma mantığı
- Basit düşman/tehlike davranışları
- Skor ve yıldızlandırma

### 1.4 Kapsam Harici (v2)
- Çevrim içi çok oyunculu
- Gerçek zamanlı PvP/PvE eşleşme
- Bulut kayıt zorunluluğu
- Gelişmiş sinematik/cutscene sistemi

---

## 2. Hedef Platform ve Teknoloji Gereksinimleri

### 2.1 Uygulama Teknolojisi
- Uygulama türü: Tarayıcı tabanlı 2D oyun
- İşaretleme: HTML5
- Stil: CSS3
- Oyun mantığı: Vanilla JavaScript (ES6+)
- Çizim katmanı: HTML5 Canvas 2D API

### 2.2 Platform Desteği
- Masaüstü tarayıcılar: Chrome, Edge, Firefox (son 2 majör sürüm)
- Minimum ekran çözünürlüğü: 1366x768
- Önerilen oyun çözünürlüğü: 960x540 (16:9)

### 2.3 Uyumluluk Politikası
- Dış kütüphane olmadan çalışabilme (bağımsız prototip)
- Tek HTML giriş dosyası üzerinden başlatılabilme
- Asset bağımlılığı minimum olacak şekilde fallback renk/şekil çizimleri

---

## 3. Oyun Tasarımı Gereksinimleri

### 3.1 Temel Döngü (Core Loop)
1. Bölüme gir
2. Alanı keşfet
3. Renk parçalarını topla
4. Tehlikelerden kaç / bulmaca çöz
5. Kapıyı aç ve bölümü bitir
6. Puan al, yıldız kazan, sonraki bölüme geç

### 3.2 Seans Döngüsü (Session Loop)
- Açılış → Kayıt seçimi → Bölüm seçimi → Oyun → Sonuç ekranı → İlerleme kaydı
- Oyuncu başarısız olursa kontrol noktası/başlangıç noktasına dönme

### 3.3 Öğretim (Onboarding)
- Bölüm 1’de hareket ve zıplama öğretimi
- Bölüm 2’de toplama ve kapı açma öğretimi
- Bölüm 3’te tehlike kaçınma öğretimi
- Her yeni mekanik için kısa metin ipucu

---

## 4. Detaylı Oyun Mekanik Gereksinimleri

### 4.1 Oyuncu Hareket Mekanikleri
- Sol/Sağ hareket (A-D veya ←-→)
- Zıplama (W/↑/Space)
- Yerde/ havada farklı hız limiti
- Düşüşte maksimum hız sınırı (terminal velocity)
- Basit sürtünme etkisi (tuş bırakınca kademeli yavaşlama)

### 4.2 Çarpışma Mekanikleri
- AABB tabanlı çarpışma
- Ayrık eksen çözümü: X ve Y düzleminde ayrı düzeltme
- Platform üstüne iniş algılama
- Tavan çarpması ve yatay itme çözümü
- Tehlike tile/nesne çarpışmalarında hasar

### 4.3 Can ve Hasar Mekanikleri
- Oyuncu başlangıç canı: 3
- Tehlikeye temas: -1 can
- Can 0 olduğunda bölüm/oturum politikası:
  - seçenek A: bölüm başına dön
  - seçenek B: oyun başına dön (tasarıma göre)
- Hasar sonrası kısa dokunulmazlık süresi (invulnerability frame): 0.75–1.25 sn

### 4.4 Kontrol Noktası (Checkpoint) Mekanikleri
- Uzun bölümlerde en az 1 checkpoint
- Checkpoint aktif olduğunda spawn güncelleme
- Yeniden doğuşta toplanan zorunlu öğelerin durumu tasarıma göre korunur ya da sıfırlanır

### 4.5 Toplanabilir Mekanikleri
- Zorunlu renk parçaları: bölüm başına minimum 3
- İsteğe bağlı bonus parçalar: 0–2
- Toplama anında görsel/işitsel geri bildirim
- Tüm zorunlu parçalar toplanmadan çıkış kapısı etkin olmaz

### 4.6 Kapı ve Geçiş Mekanikleri
- Kapı başlangıçta kilitli görselde
- Gereken öğeler tamamlanınca kapı durum değiştirir
- Kapıya etkileşim veya temas ile geçiş
- Geçişte bölüm sonu ekranı tetiklenir

### 4.7 Tehlike ve Düşman Mekanikleri
- Statik tehlikeler: diken, çukur, lazer çizgisi
- Dinamik tehlikeler: hareketli platform düşüşü, devriye düşman
- Düşman davranışları:
  - Devriye (iki nokta arası)
  - Takip (menzil içinde)
  - Zamanlı aktif/pasif

### 4.8 Bulmaca Mekanikleri
- Anahtar-kapı sistemi
- Basınç plakası ile kapı açma
- Renk eşleştirme panelleri
- Zamanlayıcı ile tetiklenen geçit
- Aynı bölümde 2+ mekanik kombinasyonu (B8-B10)

### 4.9 Fizik ve Oyun Hissi
- Yerçekimi, zıplama yüksekliği, hava kontrolü dengelenebilir parametre olmalı
- Delta-time tabanlı güncelleme önerilir
- Düşük FPS durumunda hareket sapmaları azaltılmalı

---

## 5. 10 Bölümlük Tasarım Gereksinimi (Makro)

### 5.1 Bölüm Dağılımı
- B1: Temel hareket eğitimi
- B2: Toplanabilir ve kapı
- B3: Basit tehlikeler
- B4: Devriye düşman
- B5: Anahtar-kapı + platform kombinasyonu
- B6: Zamanlayıcı bulmaca
- B7: Hareketli platformlar
- B8: Çok adımlı bulmaca
- B9: Yüksek zorluk + düşük hata payı
- B10: Final bölüm (çoklu mekanik + boss benzeri düzen)

### 5.2 Zorluk Eğrisi
- İlk 3 bölümde düşük ceza, yüksek öğrenme
- Orta bölümlerde karar verme ve refleks dengesi
- Son 2 bölümde mekanik birleşimi ve kaynak yönetimi

### 5.3 Süre Hedefi
- Bölüm başına ortalama: 4–8 dk (ilk denemede)
- Toplam ana hikâye: 60–90 dk

---

## 6. Hikâye ve Anlatı Gereksinimleri

### 6.1 Dünya Kurgusu
- Renklerin kaybolmasıyla dünyanın soluklaşması
- Her bölümde bir ana renge dair tema/ambiyans
- Finalde tüm renklerin birleşimi ile dünyanın canlanması

### 6.2 Anlatı Akışı
- Açılış metni (maksimum 4 kısa paragraf)
- Bölüm giriş metinleri (1-2 cümle)
- Final kapanış metni

### 6.3 Karakterler
- Oyuncu: isimsiz stickman kahraman
- Antagonist: Renk Hırsızı (dolaylı veya doğrudan)
- NPC (opsiyonel): Rehber ruh / bilge karakter

---

## 7. UI/UX Gereksinimleri

### 7.1 HUD Gereksinimleri
- Bölüm numarası
- Toplanan renk parçası sayısı
- Can bilgisi
- Opsiyonel: süre ve skor

### 7.2 Menü Gereksinimleri
- Ana menü: Başlat, Devam, Ayarlar, Çıkış
- Ayarlar: ses, parlaklık/kontrast, kontrol şeması
- Duraklatma menüsü: devam et, yeniden başlat, menüye dön

### 7.3 Geri Bildirim Gereksinimleri
- Hasar alınca kısa ekran efekti
- Parça toplanınca görsel parıltı
- Kapı açılınca belirgin renk değişimi
- Bölüm bitince sonuç paneli

### 7.4 Erişilebilirlik Gereksinimleri
- Yüksek kontrast modu
- Renk körlüğü dostu simge desteği
- Kritik bilgi sadece renkle ifade edilmemeli

---

## 8. Veri, Kayıt ve İlerleme Gereksinimleri

### 8.1 Kayıt Modeli
- Yerel depolama (localStorage veya dosya eşleniği)
- Kayıt içeriği:
  - Açık bölüm
  - En iyi süre
  - En iyi skor
  - Yıldız dereceleri
  - Ayarlar

### 8.2 Kayıt Güvenliği
- Bozuk kayıt algılama
- Varsayılan veriye güvenli dönüş
- Versiyonlu kayıt şeması (saveVersion)

### 8.3 İlerleme Kuralları
- Bölüm N tamamlanınca N+1 açılır
- Yeniden oynama serbest
- Daha iyi skor süresi kaydedilir

---

## 9. Ses ve Görsel Gereksinimleri

### 9.1 Ses
- Ana menü müziği
- Oyun içi fon müziği (tema varyasyonlu)
- Efektler: zıplama, hasar, toplama, kapı açılma, bölüm bitiş
- Müzik/SFX bağımsız ses seviyesi kontrolü

### 9.2 Görsel Stil
- Stickman karakter net okunabilir olmalı
- Arkaplan katmanları ile derinlik hissi
- Bölüm temasına göre renk paleti değişimi
- Kritik nesneler (tehlike/kapı/parça) ayırt edilebilir olmalı

### 9.3 Asset Yönetimi
- Asset yüklenemezse şekil tabanlı fallback
- Sprite/Audio önbellekleme
- Kaynaklar için tekil ID isimlendirme standardı

---

## 10. Teknik Mimari Gereksinimleri

### 10.1 Kod Organizasyonu
- `index.html`: kök UI + canvas
- `styles.css`: görünüm
- `game.js`: oyun döngüsü, fizik, input, seviye yönetimi
- İleride modüler ayrım (engine.js, levels.js, ui.js) için uygun yapı

### 10.2 Oyun Döngüsü
- `requestAnimationFrame` tabanlı güncelleme
- Güncelleme ve render ayrıştırması
- Duraklatıldığında update durmalı, render durumu korunmalı

### 10.3 Performans Teknikleri
- Gereksiz nesne üretimini azaltma
- Çarpışma kontrollerinde erken çıkış
- Büyük veri yapılarında yeniden kullanım (object pooling opsiyonel)

### 10.4 Hata Yönetimi
- Kritik fonksiyonlarda koruyucu doğrulama
- Konsol log seviyeleri (info/warn/error)
- Kullanıcıya kısa, anlaşılır hata mesajı

---

## 11. Performans ve Kalite Hedefleri

### 11.1 Performans
- Hedef FPS: 60
- Kabul edilebilir alt sınır: 45 FPS (ortalama)
- Bölüm yükleme süresi: ≤2 sn

### 11.2 Kararlılık
- 30+ dakika kesintisiz oynanışta crash olmamalı
- Bellek kullanımında uzun oturumda kontrolsüz artış olmamalı

### 11.3 Kalite
- Oyun kıran hata (blocker) = 0
- Kritik hata = 0
- Majör hata sınırı = yayın öncesi kapatılmalı

---

## 12. Test Gereksinimleri (Detaylı)

### 12.1 Fonksiyonel Test Senaryoları
- Karakter hareket testleri
- Zıplama/fizik testleri
- Platform çarpışma testleri
- Diken/tehlike hasar testleri
- Parça toplama ve sayaç testleri
- Kapı açılma koşulu testleri
- Bölüm geçiş testleri
- 10. bölüm bitiş ve oyun reset testleri

### 12.2 UI/UX Testleri
- HUD doğruluk testi
- Menüden oyuna ve oyundan menüye akış testleri
- Çözünürlükte taşma/bozulma testi

### 12.3 Kayıt Testleri
- İlk açılış varsayılan kayıt testi
- İlerleme kaydet/yeniden yükle testi
- Bozuk kayıt fallback testi

### 12.4 Regresyon Testleri
- Yeni mekanik eklenince eski bölümlerin doğrulanması
- Denge değişikliklerinde tempo/zorluk tekrar kontrolü

### 12.5 Kabul Testleri
- 10 bölüm oynanabilir
- Oyuncu takılmadan ana hikâyeyi bitirebilir
- Geri bildirimler anlaşılır ve yeterli

---

## 13. Dengeleme Gereksinimleri

### 13.1 Zorluk Dengeleme Parametreleri
- Zıplama yüksekliği
- Hareket hızı
- Tehlike yerleşimi
- Can sayısı
- Checkpoint sıklığı

### 13.2 Ekonomi ve Ödül
- Skor katsayıları (hız, hasarsız bitiriş, bonus toplama)
- Yıldız sistemi:
  - 1 yıldız: bitirme
  - 2 yıldız: hedef süre altı
  - 3 yıldız: hasarsız + hedef süre

---

## 14. İçerik Üretim Gereksinimleri

### 14.1 Harita İçeriği
- Her bölümde başlangıç, orta meydan okuma, final geçiş alanı
- Tekrar eden şablon hissini azaltacak varyasyonlar

### 14.2 Metin İçeriği
- Türkçe dilinde tutarlı üslup
- İpucu metinleri kısa ve eylem odaklı

### 14.3 VFX/SFX İçeriği
- Toplama, hasar, geçiş için en az birer efekt
- Aşırı görsel kalabalık oluşturmayan sade efektler

---

## 15. Güvenlik ve Dayanıklılık Gereksinimleri

- Kullanıcı girdileri güvenli şekilde ele alınmalı
- Kayıt verisi parse edilirken hata toleransı olmalı
- Oyun döngüsünü kilitleyebilecek istisnalar minimize edilmeli

---

## 16. Sürümleme ve Yayın Gereksinimleri

### 16.1 Sürümleme
- SemVer benzeri sürümleme (ör. 0.2.0)
- Değişiklik günlüğü tutulmalı

### 16.2 Yayın Paketi
- Tek klasörde çalıştırılabilir web build
- Dokümantasyon:
  - Çalıştırma talimatı
  - Kontroller
  - Bilinen kısıtlar

---

## 17. Teslimat Çıktıları

1. Oynanabilir web prototipi (HTML/CSS/JS)
2. 10 bölüm içerik tanımı
3. Güncel gereksinim dokümanı
4. Test senaryo listesi
5. Kısa teknik mimari notu

---

## 18. Başarı Kriterleri (Definition of Done)

- [ ] 10 bölüm erişilebilir ve tamamlanabilir
- [ ] Ana mekanikler hatasız çalışır (hareket, toplama, geçiş, hasar)
- [ ] Kullanıcı arayüzü temel bilgileri doğru gösterir
- [ ] Kayıt/ilerleme sistemi beklendiği gibi çalışır
- [ ] Performans hedefleri minimum seviyede karşılanır
- [ ] Kritik hata kalmaz

---

## 19. Gelecek Sürümler İçin Genişleme Önerileri

- Yeni biyomlar (gece, mağara, buz)
- Yetenek açma sistemi (çift zıplama, dash)
- Boss savaşlarının mekanik derinliğinin artırılması
- Bölüm editörü ve topluluk haritaları
- Başarım (achievement) sistemi



## 20. Mikro Mekanik Parametre Gereksinimleri
### 20.1 Hareket Parametreleri
- Yürüme ivmesi, maksimum yatay hız, hava kontrol katsayısı ve frenleme katsayısı ayrı ayrı konfigüre edilebilir olmalıdır.
- "Coyote time" (platformdan ayrıldıktan hemen sonra zıplama toleransı) 80–140 ms aralığında ayarlanabilir olmalıdır.
- "Jump buffer" (zıplama tuşuna erken basma toleransı) 80–140 ms aralığında desteklenmelidir.
- Kısa/uzun zıplama ayrımı için tuş bırakma anına göre dikey hız kırpması uygulanmalıdır.

### 20.2 Hasar Parametreleri
- Temas hasarı, alan hasarı ve düşüş hasarı ayrı parametreler olarak tanımlanmalıdır.
- Her hasar türü için bekleme süresi (cooldown) ve geri tepme (knockback) tanımlanmalıdır.
- Hasar sonrası görsel yanıp sönme süresi ve tekrar hasar alabilme süresi bağımsız ayarlanabilmelidir.

### 20.3 Kamera Parametreleri
- Kamera takip yumuşatma katsayısı (lerp factor) konfigüre edilebilir olmalıdır.
- Kamera sınırları bölüm sınırlarını aşmamalıdır.
- Düşüşlerde aşağı bakış ofseti ve yüksek zıplamada yukarı bakış ofseti desteklenmelidir.

## 21. Gelişmiş Bölüm Mekanikleri
### 21.1 Çevresel Etkileşimler
- Kırılabilir bloklar: belirli etkiyle yok olur, yol açar.
- Tek yönlü platformlar: alttan geçiş, üstten temas.
- Kaygan yüzeyler: düşük sürtünme, kontrol zorluğu.
- Rüzgar alanları: yatay/dikey kuvvet uygular.

### 21.2 Anahtar ve Kapı Varyasyonları
- Tek anahtarlı kapılar
- Sıralı anahtar kapılar (A anahtarı alınmadan B aktif olmaz)
- Zamanlı anahtar kapılar (süre bitince kapanır)
- Renk kodlu kapılar (ilgili renk parçası şartı)

### 21.3 Mantık Bulmacaları
- Switch kombinasyonu (doğru sıralama)
- Ayna/ışık yönlendirme
- Ağırlık tabanlı platform dengesi
- Sinyal aktarımı (A tetikleyici B’yi aktif eder)

## 22. Düşman Yapay Zekâ Gereksinimleri
### 22.1 Algı Sistemi
- Görüş konisi açısı, görüş mesafesi, tepki gecikmesi parametre olmalıdır.
- Oyuncu görüşten çıkınca arama süresi tetiklenmelidir.
- Ses/olay tabanlı dikkat çekme (opsiyonel) desteklenmelidir.

### 22.2 Davranış Durumları
- Idle, Patrol, Alert, Chase, Return durumları tanımlanmalıdır.
- Durum geçişleri açık kurallarla FSM (finite state machine) mantığıyla uygulanmalıdır.
- Her düşman türü için farklı hız/atak gecikmesi desteklenmelidir.

### 22.3 Savaş/Etkileşim
- Prototipte doğrudan savaş yoksa bile oyuncuya temas hasarı net olmalıdır.
- İleri sürümde yakın temas/menzilli tehdit genişlemesi için arayüz noktaları bırakılmalıdır.

## 23. İlerleme, Ödül ve Meta Sistem Gereksinimleri
### 23.1 Puanlama Formülü
- Puan = temel tamamlama puanı + hız bonusu + hasarsız bonus + bonus parça bonusu.
- Her bölüm için hedef süre eşikleri (altın/gümüş/bronz) ayrı tanımlanmalıdır.

### 23.2 Yıldızlandırma
- 1 yıldız: bölüm bitirme
- 2 yıldız: hedef sürede bitirme
- 3 yıldız: hasarsız + hedef sürede + tüm bonuslar
- Aynı bölüm tekrarında daha yüksek yıldız öncekinin yerine yazılmalıdır.

### 23.3 Kilit Açma Kuralları
- Ana hikâye için doğrusal açılma zorunluluğu.
- Opsiyonel gizli kapılar için toplam yıldız eşiği.
- Final bölüm için minimum renk parçası + minimum yıldız kriteri.

## 24. Kullanıcı Deneyimi Derin Gereksinimleri
### 24.1 Giriş Gecikmesi
- Input-to-action gecikmesi tek kareyi aşmamalı (normal koşullarda).
- Tuş tekrar oranından etkilenmeyecek şekilde event + state birleşik yönetim olmalıdır.

### 24.2 Görsel Okunabilirlik
- Foreground ile background kontrastı WCAG’e yakın okunabilirlik hedeflemelidir.
- Tehlikeler çevreden net ayrışmalıdır (renk + şekil).
- Etkileşimli nesnelerde animasyon veya pulse ile dikkat yönlendirme yapılmalıdır.

### 24.3 Öğrenme Eğrisi
- İlk 10 dakikada tüm temel kontroller oyuncuya gösterilmiş olmalıdır.
- Yeni mekanik eklenen bölümde önce risksiz mini deneme alanı sağlanmalıdır.

## 25. Menü ve Durum Ekranı Gereksinimleri
### 25.1 Sonuç Ekranı
- Bölüm süresi
- Alınan hasar sayısı
- Toplanan zorunlu/bonus parçalar
- Kazanılan yıldız
- Bir sonraki bölüm butonu

### 25.2 Oyun Sonu Ekranı
- Toplam süre
- Toplam yıldız
- Tamamlanan bölümler
- Hikâye kapanış metni
- Yeniden oynama çağrısı

### 25.3 Ayar Ekranı
- Tuş atama (rebind) desteği
- Ses kaydırıcıları (0-100)
- Efekt yoğunluğu (düşük/orta/yüksek)
- Kontrast modu seçimi

## 26. Veri Şeması Gereksinimleri
### 26.1 Bölüm Verisi Şeması
- `id`, `name`, `themeColor`, `spawn`, `platforms`, `hazards`, `collectibles`, `door`, `checkpoints`, `timers` alanları zorunlu olmalıdır.
- Şema doğrulaması yükleme anında yapılmalıdır.

### 26.2 Kayıt Verisi Şeması
- `saveVersion`, `unlockedLevel`, `bestTimes`, `bestScores`, `stars`, `settings` alanları zorunlu olmalıdır.
- Eksik alanlar için varsayılan değer enjekte edilmelidir.

### 26.3 Migrasyon
- `saveVersion` düşükse migrasyon adımları çalıştırılmalı.
- Migrasyon başarısızsa veri yedeklenip varsayılan profile dönülmelidir.

## 27. Teknik Borç ve Refaktör Planı Gereksinimleri
- Tek dosya prototip kodu için modülerleşme backlog’u tutulmalıdır.
- Oyun motoru katmanı ile içerik katmanı ayrıştırılmalıdır.
- Render, input, physics, level systems bağımsız test edilebilir hale getirilmelidir.

## 28. Test Tasarımı Genişletmesi
### 28.1 Birim Test Kapsamı (hedef)
- Çarpışma fonksiyonları
- Puan hesaplama fonksiyonu
- Seviye geçiş koşulu
- Kayıt doğrulama/migrasyon

### 28.2 Senaryo Testleri
- Oyuncu parçaları farklı sırada toplayınca kapı mantığı doğru çalışmalı.
- Checkpoint sonrası ölümde doğru noktaya dönüş olmalı.
- Hasar cooldown süresinde ardışık temaslar tek hasar sayılmalı.

### 28.3 Dayanıklılık Testleri
- 100 seviye reset döngüsünde bellek büyümesi kabul sınırında kalmalı.
- 60 dakika açık oturumda input ve fizik stabil kalmalı.

## 29. Telemetri ve Analitik Gereksinimleri (Opsiyonel)
- Bölüm başına ölüm noktası ısı haritası verisi
- Ortalama bitirme süresi
- En çok başarısız olunan bulmaca türü
- Ayar kullanım dağılımı

## 30. Erişilebilirlik ve Kapsayıcılık Genişletmesi
- Tek elle oynanabilir alternatif kontrol düzeni.
- Kritik olaylar için görsel + metin + (opsiyonel) sesli uyarı.
- Renk körlüğü presetleri: Protanopia, Deuteranopia, Tritanopia için tema alternatifleri.

## 31. İçerik Ölçekleme Gereksinimleri
- Yeni bölüm eklemek için yalnızca seviye verisi eklenerek çalışabilecek altyapı hedeflenmelidir.
- Mekaniklerin veri odaklı aç/kapa yönetimi desteklenmelidir.
- Yeni tehlike tipi eklemek için render + collision + behavior sözleşmeleri standartlaştırılmalıdır.

## 32. Riskler ve Azaltma Planı
- Risk: Tek dosyada kod karmaşıklığı artışı → Azaltma: modüler refaktör milestone’u.
- Risk: Performans dalgalanması → Azaltma: profiling + object reuse.
- Risk: Zorluk dengesizliği → Azaltma: playtest metrikleri ve iteratif tuning.
- Risk: Kayıt bozulması → Azaltma: şema doğrulama + yedekleme.

## 33. Operasyonel Kabul Kriterleri
- Yayın öncesi manuel test checklist %100 tamamlanmalı.
- Bloker/kritik hata kapanmadan sürüm etiketi vurulmamalı.
- Her sürümde değişiklik notu ve bilinen sorunlar listesi yayınlanmalı.

## 34. Üretim Yol Haritası (Detay)
1. Milestone A: Çekirdek motor stabilizasyonu (hareket/çarpışma/fizik)
2. Milestone B: Mekanik derinleştirme (checkpoint, bulmaca, düşman FSM)
3. Milestone C: İçerik tamamlama (10 bölüm kalite geçişi)
4. Milestone D: UX, erişilebilirlik, performans iyileştirmeleri
5. Milestone E: Test kapanışı, sürümleme, yayın hazırlığı

## 35. Nihai Başarı Tanımı
- Oyuncuların en az %70’i ilk 3 bölümü yardımsız tamamlayabilmeli.
- Oyuncuların en az %40’ı ilk oturumda 5. bölüme ulaşabilmeli.
- Kritik oynanış hatası rapor oranı playtest başına 0’a yakın olmalı.
- Teknik hedefler (FPS, yükleme, kararlılık) doğrulama raporuyla kanıtlanmalı.
