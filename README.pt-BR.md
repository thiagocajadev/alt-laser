<div align="center">
  <img src="docs/logo.png" width="128" alt="Logo do Alt Laser" />
  <h1 align="center">Alt Laser</h1>
  <p align="center">Um ponteiro laser para a tela inteira. Segure o Alt, aponte e ele some sozinho.</p>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/licen%C3%A7a-MIT-98C379?style=flat-square" alt="Licença MIT" /></a>
  <a href="../../releases/latest"><img src="https://img.shields.io/github/v/release/thiagocajadev/alt-laser?style=flat-square&label=vers%C3%A3o&color=blue" alt="Versão publicada" /></a>
  <a href="../../releases/latest/download/AltLaser.exe"><img src="https://img.shields.io/badge/baixar-Windows-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Baixar para Windows" /></a>
  <br><br>
  <a href="README.md">English</a> · <b>Português (Brasil)</b>
</div>

<br>

Feito para apresentações, aulas e compartilhamento de tela no Windows: aponte para o que importa e
a marcação some sozinha, sem nada para desfazer.

<p align="center">
  <img src="docs/demo.gif" width="640" alt="Setas desenhadas na área de trabalho com o Alt Laser, sumindo sozinhas" />
</p>

## Instalar

Windows 10 ou 11 (64 bits). Não precisa instalar mais nada.

1. Baixe o [`AltLaser.exe`](../../releases/latest/download/AltLaser.exe) e salve onde quiser.
2. Abra o arquivo. O Alt Laser fica na bandeja do sistema, pronto para usar.

Na primeira vez, o Windows pode mostrar o aviso abaixo do SmartScreen, porque o app é novo e ainda
não tem assinatura digital. Clique em **Executar assim mesmo** (em algumas versões, clique antes em
**Mais informações**). Todo o código-fonte está neste repositório, e você pode compilar o app se
preferir.

<p align="center">
  <img src="docs/smartscreen-pt-BR.png" width="420" alt="Aviso do SmartScreen com o botão Executar assim mesmo" />
</p>

## Usar

- **Segure o Alt esquerdo** e mova o mouse para desenhar uma seta. Ao soltar, a seta some.
- **`Alt+L`** desliga e liga o Alt Laser.
- **Clique com o botão direito no ícone da bandeja** para alternar entre **Seta** e **Livre**,
  escolher a **Cor**, quanto tempo os traços ficam (**Sumir após**) ou trocar o **Idioma**. As
  escolhas ficam salvas.

<p align="center">
  <img src="docs/menu-pt-BR.png" alt="Menu do Alt Laser na bandeja: Desligar, Seta, Livre, Cor, Sumir após, Idioma, Sobre e Sair" />
</p>

Atalhos como `Alt+Tab` continuam funcionando normalmente, e o Alt direito (AltGr) nunca é usado.

## Privacidade e segurança

- Funciona só no seu computador: sem internet, sem conta e sem coleta de dados.
- Não precisa de permissão de administrador.
- Só reage ao Alt esquerdo. Nada do que você digita é gravado ou enviado.
- O cursor de laser é temporário: o cursor normal volta quando você solta o Alt ou fecha o app.

<details>
<summary><b>Iniciar com o Windows</b></summary>

<br>

Aperte `Win+R`, digite `shell:startup` e dê Enter. Coloque um atalho para o `AltLaser.exe` na
pasta que abrir.

</details>

<details>
<summary><b>Desinstalar</b></summary>

<br>

Clique com o botão direito no ícone da bandeja → **Sair** e apague o `AltLaser.exe`. Para remover
também as escolhas salvas, apague a pasta `%AppData%\AltLaser`.

</details>

<details>
<summary><b>Compilar o código-fonte</b></summary>

<br>

Requer o [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet run --project src/AltLaser
dotnet publish src/AltLaser -p:PublishProfile=SingleExe   # publish/AltLaser.exe
```

O atalho, os tamanhos e outros ajustes finos ficam em [`LaserSettings.cs`](src/AltLaser/LaserSettings.cs).

</details>

## Créditos

Inspirado no laser do [Excalidraw](https://excalidraw.com), uma ferramenta que eu uso e recomendo.
O Alt Laser é um projeto independente e não tem nenhuma ligação com o Excalidraw.

## Licença

MIT, veja [`LICENSE`](LICENSE).
