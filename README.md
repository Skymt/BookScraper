# BookScraper

## Beskrivning
Flertrådad web-scraper hårdkodad för http://books.toscrape.com/.

### Program.cs
Entrypoint för programmet. Startar upp scraper, och erbjuder möjlighet att starta den lokala kopian.

Det är även här man anger hur många trådar som ska användas, samt var den lokala kopian ska sparas.

### Scraper.cs
Läser html, js och css-filer från site, och söker efter länkar till andra sådana filer.
Den söker även efter bilder, men dessa läses inte, utan sparas endast ner till hårddisk.
Process-funktionen har en timer som periodiskt visar hur många filer som ligger i kö.
Därutöver rapporterar varje tråd till Console.



## Tankar angående anställningsprov
### Instruktioner
Mycket tydliga, och tack vare nya siten lättare att följa. Länkstrukturen lämpar sig bättre
för att spara under file://, så ingen manipulation av källkodsfilerna behövs längre!
  
### Uppgiften från provtagarens perspektiv
Intressant och lagomt utmanande. Det märks att siten är anpassad för scraping, då länkstruktur
är mer homogen i det tidigare testet. Att pussla ihop relativa url:er med aktuell fil var en 
intressant utmaning, jag hade lite svårt att bestämma mig för om jag skulle använda arrays, 
regex eller direkt strängmanipulering. (Det vart mest läsbart med arrays dock, och C#s nya
range-operator) :) 

### Uppgift från utvärderarens perspektiv
Mycket bra uppgift som testar både problem-lösning och kod-vana och belyser provtagarens
erfarenhet att göra verktyg, speciellt ifall de inte använder externa bibliotek.
Det kändes lite konstigt att göra ett test-projekt dock. Hade jag behövt skrapa fler siter
hade jag valt något verktyg, och mot en site så är man ju så beroende av den. Bra ämne för
diskussion under review dock! :) Jag hade brutit ut reader för att hitta strängar inom
citationstecken, konkateneringen av url:er, men jag hade nog skippat sådana som beror på 
http://books.toscrape.com/...
  
