# BookScraper

## Beskrivning
Flertr�dad web-scraper h�rdkodad f�r http://books.toscrape.com/.

### Program.cs
Entrypoint f�r programmet. Startar upp scraper, och erbjuder m�jlighet att starta den lokala kopian.

Det �r �ven h�r man anger hur m�nga tr�dar som ska anv�ndas, samt var den lokala kopian ska sparas.

### Scraper.cs
L�ser html, js och css-filer fr�n site, och s�ker efter l�nkar till andra s�dana filer.
Den s�ker �ven efter bilder, men dessa l�ses inte, utan sparas endast ner till h�rddisk.
Process-funktionen har en timer som periodiskt visar hur m�nga filer som ligger i k�.
D�rut�ver rapporterar varje tr�d till Console.



## Tankar ang�ende anst�llningsprov
### Instruktioner
Mycket tydliga, och tack vare nya siten l�ttare att f�lja. L�nkstrukturen l�mpar sig b�ttre
f�r att spara under file://, s� ingen manipulation av k�llkodsfilerna beh�vs l�ngre!
  
### Uppgiften fr�n provtagarens perspektiv
Intressant och lagomt utmanande. Det m�rks att siten �r anpassad f�r scraping, d� l�nkstruktur
�r mer homogen i det tidigare testet. Att pussla ihop relativa url:er med aktuell fil var en 
intressant utmaning, jag hade lite sv�rt att best�mma mig f�r om jag skulle anv�nda arrays, 
regex eller direkt str�ngmanipulering. (Det vart mest l�sbart med arrays dock, och C#s nya
range-operator) :) 

### Uppgift fr�n utv�rderarens perspektiv
Mycket bra uppgift som testar b�de problem-l�sning och kod-vana och belyser provtagarens
erfarenhet att g�ra verktyg, speciellt ifall de inte anv�nder externa bibliotek.
Det k�ndes lite konstigt att g�ra ett test-projekt dock. Hade jag beh�vt skrapa fler siter
hade jag valt n�got verktyg, och mot en site s� �r man ju s� beroende av den. Bra �mne f�r
diskussion under review dock! :) Jag hade brutit ut reader f�r att hitta str�ngar inom
citationstecken, konkateneringen av url:er, men jag hade nog skippat s�dana som beror p� 
http://books.toscrape.com/...
  
