# OnlineShop 
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9-0D6EFD?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-334155?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-8-00758F?style=for-the-badge&logo=mysql&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5-0D6EFD?style=for-the-badge&logo=bootstrap&logoColor=white)
![C%23](https://img.shields.io/badge/C%23-10-0EA5E9?style=for-the-badge&logo=csharp&logoColor=white)
Un online shop pentru produse Apple, cu UI minimalist inspirat de cultura celor de la Apple, construit în **ASP.NET Core MVC**. Aplicația include autentificare cu roluri, coș, wishlist, comenzi, review-uri și un “Product Assistant” cu logare de întrebări și FAQ.

**Studenți:** Alin-Ovidiu Iacob, Eva-Cristiana Diaconescu  
**Profesor coordonator:** Lect. Univ. Dr. Cezara Benegui

**Stack:** ASP.NET Core MVC · Entity Framework Core · ASP.NET Identity · Bootstrap · C#

---

## Despre proiect

OnlineShop simulează experiența unui magazin online modern pentru produse Apple: browse rapid, filtre, sortare, pagini de produs clare, apoi un flow complet pentru utilizatori autentificați (wishlist, coș, comandă).  
În zona de administrare există flux de aprobare pentru produse propuse de colaboratori și un modul de FAQ pentru a susține componenta AI.

---

## Highlights
- Roluri și acces controlat (Visitor, User, Editor, Admin)
- Coș + wishlist per utilizator, cu validare stoc și flow de checkout
- Review-uri + rating, cu scor mediu calculat
- Căutare, filtrare, sortare (preț și rating)
- Product Assistant bazat pe descriere + FAQ, cu logare întrebări
- Admin panels: aprobări produse, gestionare utilizatori, top întrebări și FAQ

---

## Funcționalități

### Experiența de cumpărare
- Listare produse și rating vizibil
- Pagina produsului cu detalii, coș, wishlist și review-uri
- Coș: update cantitate, subtotal, total
- Comenzi: istoric comenzi cu total pe comandă
- Wishlist: fără duplicare, mutare rapidă în coș

### Conturi și roluri
- Autentificare și înregistrare cu ASP.NET Identity
- Acces diferențiat:
  - Vizitator: doar vizualizare
  - User: coș, wishlist, comenzi, review-uri
  - Editor: propune produse și își gestionează produsele aprobate de administrator
  - Admin: gestionare completă (produse, categorii, utilizatori, aprobări, FAQ)

### Produse și categorii
- Validări pentru câmpuri esențiale (preț, stoc, categorie)
- Încărcare imagine produs cu restricții de tip
- Status produs: Pending, Approved, Rejected
- Flux de aprobare și feedback pentru propuneri (Editor → Admin)

### Review-uri și rating
- Rating între 1 și 5 (opțional)
- Comentariu text (opțional)
- Media ratingurilor calculată automat și afișată în UI

### Căutare, filtrare, sortare
- Căutare după denumire cu potrivire parțială
- Filtrare pe categorii (inclusiv din meniul de categorii)
- Sortare după preț și rating, crescător/descrescător

### Product Assistant (AI)
- Chat lateral pe pagina produsului
- Răspunsuri bazate pe descriere + FAQ
- Logare a întrebărilor frecvente în baza de date
- Administrare FAQ pentru produs și vizualizare “Top întrebări”

---

## Detalii tehnice

- **EF Core + Query Filters** pentru soft delete 
- Protecție istoric comenzi: OrderItem rămâne valid chiar dacă produsul nu mai este “în shop”
- Validare stoc la adăugare în coș și la checkout
- UI consistent: Bootstrap + theme custom (apple-theme)

---

## Cum rulezi proiectul local

### Cerințe
- .NET 9 SDK 
- EF Core Tools
- MySql/SqlServer
