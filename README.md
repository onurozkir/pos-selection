# PayTR POS Selection Service

Bu proje, PayTR için tasarlanmış **POS Seçim Servisi** örneğidir.  
Amaç, e-ticaret ödeme anında müşterinin kart bilgilerine ve tutara göre **en düşük maliyetli POS sağlayıcısını** seçip işlemi o POS’a yönlendirmektir.

Proje üç ana bileşenden oluşur:

- **API**: POS seçimi yapan Minimal API (`POST /v1/pos-selection`)
- **Job**: Günlük POS oranlarını mock API’den çekip Redis + PostgreSQL’e yazan Quartz job
- **Infrastructure**: PostgreSQL, Redis ve ortak data erişimi katmanı 

---

## İçindekiler

- [Dış Kaynak / Mock API](#dis-kaynak-mock-api)
- [Pos Seçim Algoritması](#pos-seçim-algoritması)
- [Kullanılan Teknolojiler](#kullanılan-teknolojiler)
- [Kurulum](#kurulum)
    - [Ön Gereksinimler](#ön-gereksinimler)
    - [Docker ile Çalıştırma](#docker-ile-çalıştırma)
- [API Kullanımı](#api-kullanımı)
    - [Endpoint](#endpoint)
    - [Örnek İstekler](#örnek-istekler)
- [Mimari Bakış](#mimari-bakış)

---

## Dış Kaynak / Mock API

Endpoint: https://6899a45bfed141b96ba02e4f.mockapi.io/paytr/ratios
- Dönen şema:
```text
{
"pos_name": "Garanti",
"card_type": "credit",
"card_brand": "bonus",
"installment": 6,
"currency": "TRY",
"commission_rate": 0.027,
"min_fee": 0.00,
"priority": 0
}
```
- Alan Açıklamaları:
```text
pos_name: POS sağlayıcısının adı
card_type: Kart tipi (credit/debit)
card_brand: Kart markası (bonus, maximum vb.)
installment: Taksit sayısı
currency: Para birimi (TRY, USD vb.)
commission_rate: Komisyon oranı (0.027 = %2.7)
min_fee: Uygulanabilecek en düşük işlem ücreti. Hesaplanan komisyon min_fee’den
küçükse, min_fee uygulanır.
priority: Eşit maliyet durumunda öncelik değeri (yüksek olan tercih edilir)

```

## Pos Seçim Algoritması

 
- Girdi: amount (zorunlu), installment (zorunlu), currency (zorunlu), card_type,
card_brand (opsiyonel)
- Maliyet Formülü:
```text
TRY: cost = max(amount * commission_rate, min_fee)
USD: cost = max(amount * commission_rate * 1.01, min_fee)
```

- Öncelik sırası:
```text
Eşitlik durumunda bir sonraki adıma göre karar verilecektir.
1- En düşük cost
2- Daha yüksek priority
3- Daha düşük commission_rate
4- Alfabetik pos_name
```

## Kullanılan Teknolojiler

### Uygulama Katmanı

- .NET 9 Minimal API
- FluentValidation (request validation)
- Quartz.NET (scheduled job)

### Database & Caching

- **PostgreSQL**
- **Redis**

### Resilience

- Polly Circuit Breaker (Redis / DB timeout veya connection hatalarında)
- Redis tabanlı IP Rate Limiting
- `CancellationToken` + timeout’lar (işlem süresi kontrolü)

### Observability

- Health Checks (`/health/live`, `/health/ready`)
- OpenTelemetry + Prometheus (metrik toplama)

### Containerization

- Docker & Docker Compose
  - `api` → `PayTR.PosSelection.API`
  - `jobs` → `PayTR.PosSelection.Jobs`
  - `postgres`
  - `redis`
  - `otel-collector`
  - `prometheus`

## Kurulum

### Ön Gereksinimler

- .NET SDK 9.x
- Docker
- Docker Compose

### Docker ile Çalıştırma

1. **Projeyi klonlayın:**

   ```bash
   git clone https://github.com/onurozkir/pos-selection.git pos-selection  

      cd pos-selection
   ```

2. **Config:**

   `docker-compose.yml` aşağıdakileri güncelle:

  - PostgreSQL:
    - POSTGRES_USER
    - POSTGRES_PASSWORD
    - POSTGRES_DB 
  - API için:
    - ConnectionStrings__Postgres
    - ConnectionStrings__Redis
    - PosSelectionMultiplier__TRY
    - PosSelectionMultiplier__USD
  - Job için:
    - ConnectionStrings__Postgres
    - ConnectionStrings__Redis
    - RatiosJob__Cron
    - RatiosJob__RatiosApiUrl
    - RatiosJob__HttpTimeoutSeconds

3. **build + run:**

   ````bash 
   docker compose build --no-cache
   docker compose up
   ````

   - NOT: init.sql kodları docker/postgres/init.sql içindedir docker ile migrate edilecektir

4. **test:**

   ````bash 
   dotnet test
   ````

5. **Container’lar ayağa kalktıktan sonra:**

  - API: `http://localhost:5000`
    
    ```json
    // cURL for Postman
     
    curl --location 'http://localhost:5000/v1/pos-selection' \
    --header 'Content-Type: application/json' \
    --data '{ "amount": 362.22, "installment": 6, "currency": "TRY", "card_type": "credit" }
    '
  
    ```

  - HealthCheck endpoint’leri:
    - GET `/health/live`
    - GET `/health/ready`



---

## API Kullanımı

### Endpoint

    POST /v1/pos-selection

### Request Model

    {
      "amount": 362.22,
      "installment": 6,
      "currency": "TRY",
      "card_type": "credit",
      "card_brand": "bonus"
    }

### Response Model (Örnek)

    {
      "filters": {
        "amount": 362.22,
        "installment": 6,
        "currency": "TRY",
        "card_type": "credit",
        "card_brand": "bonus"
      },
      "overall_min": {
        "pos_name": "KuveytTurk",
        "card_type": "credit",
        "card_brand": "saglam",
        "installment": 6,
        "currency": "TRY",
        "commission_rate": 0.026,
        "price": 9.42,
        "payable_total": 371.64,
        "priority": 4
      }
    }

### Validasyon Kuralları

FluentValidation ile (özetle) şu kurallar uygulanır:

- `amount` > 0 olmalıdır.
- `installment` > 0 olmalıdır.
- `currency` boş olamaz (`TRY` ve `USD`).
- `card_type` ve `card_brand` alanları opsiyoneldir; dolu olduklarında formatları kontrol edilir.


## Mimari Bakış

- API

  Projeyi domain/use-case driven bir şekilde kurguladım. 
  POS seçimi business kuralını `PosSelectionService` içinde topladım; web (API), jobs ve infra’yı ayrı katmanlara alarak Clean Architecture’a yakın, domain merkezli bir yapı kurdum.

- Job

  POS oranlarının güncellenmesini time-driven bir job ile çözdüm; POS seçiminin kendisi ise data-driven. Behaviour bir internal API’den gelen ratios ve Redis ve PostgreSQL’de tutulan snapshot’lar üzerinden yönetiliyor; kodu değiştirmeden multiplier oranları değişebiliyor. 

- Maliyet Hesabı

  POS maliyet hesabını currency’e göre SOLID yapmak için factory pattern kullanıldı
  Ayrıca Multiplier değerleri configten okunarak "config-driven" bir yapı kuruldu.