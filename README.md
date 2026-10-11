# 🌊 Maré Reversa (C# + Blazor WebAssembly)

Jogo 2D feito 100% em **C#**. A lógica fica em `Engine/`, a tela em `Jogo.razor` (HTML + SVG) e o visual em `wwwroot/css/app.css`.
O único JavaScript é um mini script em `wwwroot/index.html` que repassa o teclado para o C#.

## Rodar no seu computador
1. Instale o **.NET 8 SDK**: https://dotnet.microsoft.com/download
2. Na pasta do projeto: `dotnet run`
3. Abra o endereço mostrado no terminal (algo como http://localhost:5000).

## Colocar num site (grátis)
**GitHub Pages (automático):**
1. Crie um repositório no GitHub e envie esta pasta (branch `main`).
2. Em *Settings → Pages*, escolha **Source: GitHub Actions**.
3. A cada push o arquivo `.github/workflows/deploy.yml` compila e publica. O link será `https://SEU-USUARIO.github.io/NOME-DO-REPO/`.

**Outras opções (Netlify, Azure Static Web Apps, Cloudflare Pages...):**
`dotnet publish -c Release -o release` e envie a pasta `release/wwwroot`. Fora do GitHub Pages o `<base href="/" />` do `index.html` já serve.

## Como jogar
- **Setas / WASD**: andar. **Espaço**: lançar a vara e, quando surgir o aviso, puxar. **B**: loja.
- No celular há botões na tela.
- Blocos piscando em vermelho vão alagar. Se você estiver em cima, perde uma vida.

| Peixe | Efeito |
|---|---|
| Peixe-Terra (verde) | Transforma o mar em grama |
| Peixe-Dourado (amarelo) | +6 moedas |
| Peixe-Relógio (azul) | Congela a maré por 6s |
| Peixe-Pedra (cinza) | Cria rocha que nunca alaga |
| Baiacu (vermelho, ⚠) | Faz um bloco alagar. Deixe fugir! |

**Objetivos:** 5 fases com 3 missões cada (ter X blocos de terra, pescar tipos específicos, combos, sobreviver, juntar moedas). Cada missão paga moedas, e a cada fase a maré fica mais rápida. Gaste moedas na **loja** (vara veloz, botas de borracha, âncora do tempo, coração extra). O recorde fica salvo no navegador.

## Estrutura
```
Engine/Models.cs      enums, peixes, missões, upgrades
Engine/GameEngine.cs  regras: maré, pesca, vidas, missões, fases, loja
Jogo.razor            interface (SVG gerado em C#) e laço do jogo
Program.cs            inicialização do Blazor
wwwroot/              index.html e CSS
```
