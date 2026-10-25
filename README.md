# NFC Müşteri Değerlendirme — ASP.NET Core MVC + SQL Server

ASP.NET Core 8 MVC, Entity Framework Core ve SQL Server ile yazılmış çok firmalı NFC değerlendirme platformu. Müşteri NFC karta telefonunu okutur, yıldız verir, yorum yazar. 4–5 yıldız verenler işletmenin Google yorum sayfasına yönlendirilir (yorumu müşteri kendisi yazar, uygulama Google'a yorum göndermez); 1–3 yıldız yalnızca işletme panelinde görünür.

## Özellik durumu
| Özellik | Durum |
|---|---|
| `/r/<kod>` değerlendirme sayfası: yıldız, yorum, önizleme, çift gönderim engeli | ✅ |
| 4–5 yıldız → Google bağlantısı (doğrulanmış), 1–3 → asla | ✅ |
| Panel: ziyaret, ortalama, dağılım, dönüşüm, arama, yıldız filtresi, CSV | ✅ |
| Ayarlar: Google bağlantısı, tema rengi, yorum zorunluluğu; profil fotoğrafı yükleme | ✅ |
| Süper admin: firma ekleme, pasifleştirme, arşivleme | ✅ |
| Cookie giriş, roller, firma veri izolasyonu, hız sınırı, CSRF belirteci, güvenlik başlıkları | ✅ |
| Kart yönetimi ekranı, QR kod, yorum notu/durum, şube, e-posta bildirimi, şifre değiştirme | ⏳ Yapılmadı |

## Kurulum (Windows)
Gereksinimler: [.NET 8 SDK](https://dotnet.microsoft.com/download), SQL Server (Express/LocalDB/Developer) ve SSMS.

```bat
cd NfcDegerlendirme
dotnet tool install --global dotnet-ef
```
1. **Bağlantı dizesi:** `appsettings.json` içindeki `ConnectionStrings:Default` değerini kendi SQL Server'ınıza göre düzenleyin. Varsayılan: `Server=localhost;Database=NfcDegerlendirme;Trusted_Connection=True;TrustServerCertificate=True` (Windows kimlik doğrulaması). SQL Express için `Server=localhost\SQLEXPRESS;...`
2. **İlk süper admin** (şifre dosyaya yazılmaz, kullanıcı sırlarında tutulur):
```bat
dotnet user-secrets set "SeedAdmin:Email" "admin@ornek.com"
dotnet user-secrets set "SeedAdmin:Password" "GucluBirSifre-123!"
```
3. **Veritabanı:**
```bat
dotnet ef migrations add Init
dotnet ef database update
```
4. **Çalıştır:** `dotnet run` → `http://localhost:5080/giris`. İlk açılışta süper admin oluşturulur.
5. Süper admin olarak **Yeni firma ekle** formunu doldurun; NFC adresi ekranda çıkar (`AppUrl/r/<kod>`). Üretimde `AppUrl` değerini gerçek alan adınıza çevirin.

Testler: `cd ..\NfcDegerlendirme.Tests && dotnet test`

## NFC karta yazma
NFC Tools → Yaz → URL/URI → `https://siteadi.com/r/<kart-kodu>`. Kartlar internetten erişilebilir adres gerektirir.

## Google bağlantısı
Panelde Ayarlar → "Google yorum bağlantısı". Yalnızca `https` ve Google alan adları kabul edilir. Boşsa 4–5 yıldız verenler yönlendirilmez, değerlendirme yine kaydedilir.

## Sunucuya dağıtım (IIS)
1. Sunucuya .NET 8 Hosting Bundle kurun, `dotnet publish -c Release -o publish` çıktısını IIS sitesine koyun, HTTPS bağlayın.
2. Ortam değişkenleri / IIS yapılandırması: `ConnectionStrings__Default`, `AppUrl`, `SeedAdmin__Email`, `SeedAdmin__Password` (ilk çalıştırmadan sonra seed değişkenlerini kaldırın), `UploadDir` (kalıcı bir klasör).
3. Veritabanını güncelleyin: `dotnet ef database update --connection "<bağlantı dizesi>"` veya `dotnet ef migrations script` çıktısını SSMS'te çalıştırın.
4. Ters vekil arkasındaysanız `ForwardedHeaders` yalnızca güvenilir proxy'den kabul edilir; vekilin IP'sini `KnownProxies` olarak eklemeniz gerekebilir.

## Güvenlik özeti
BCrypt şifre özeti · HttpOnly + SameSite=Strict + Secure çerez · pasif kullanıcı/firma oturumu anında düşer · her POST'ta antiforgery belirteci · EF Core parametreli sorgular · girdi temizleme · giriş ve değerlendirme için hız sınırı, honeypot, tekrar gönderim engeli · açık yönlendirme koruması · logo yüklemede imza kontrolü, SVG reddi · CSV formül koruması · CSP ve güvenlik başlıkları.

**Sınırlamalar:** hız sınırı ve tekrar kontrolü bellek içidir (birden fazla sunucuda paylaşılmaz); bağımsız güvenlik denetiminden geçmemiştir; KVKK saklama/silme süreçleri otomatik değildir.
