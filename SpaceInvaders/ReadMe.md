Clone de Space Invaders (Primeira Fase)
Este projeto é uma recriação simplificada do clássico jogo de arcade Space Invaders, desenvolvido como parte do curso de programação. O foco desta primeira versão é implementar as mecânicas fundamentais do jogo em uma estrutura de código limpa, organizada e orientada a objetos.


✨ Funcionalidades
Tela Inicial Completa: Menu com opções para "Novo Jogo", "Placares" e "Controles".

Sistema de Placares: As 10 melhores pontuações são salvas em um arquivo de texto, persistindo entre as sessões de jogo.

Inimigos Estáticos: Os aliens aparecem em formação e não se movem, conforme os requisitos da primeira fase.

Ondas de Inimigos: Ao destruir todos os aliens de uma onda, uma nova é gerada.

Controles do Jogador: Movimentação horizontal completa e capacidade de atirar (um tiro por vez na tela).

Escudos Destrutíveis: Quatro escudos protegem o jogador e se deterioram visualmente com o impacto dos tiros.

Efeitos Sonoros: Cada ação principal (tiro, morte de inimigo, início do jogo) possui um som correspondente.

Música de Fundo: A tela inicial possui uma música tema que para ao iniciar o jogo.

Condição de Vitória: O jogo termina quando o jogador atinge 500 pontos.

🚀 Tecnologias Utilizadas
C# e .NET: Linguagem e plataforma principal para toda a lógica do jogo.

Uno Platform (com WinUI 3): Framework utilizado para a criação da interface gráfica de usuário (UI) de forma multiplataforma.

NAudio: Biblioteca de áudio de terceiros utilizada para garantir a reprodução robusta e confiável dos efeitos sonoros e da música.

📂 Estrutura do Projeto
O código foi refatorado para seguir os princípios da Programação Orientada a Objetos, separando as responsabilidades em diferentes classes e pastas para melhor organização e manutenibilidade.

📁 SpaceInvaders/
|
|-- 📁 Assets/
|    |-- 📁 Images/
|
|-- 📁 Models/
|    |-- 📄 Barrier.cs      (Define um escudo com sua vida)
|    |-- 📄 Enemy.cs        (Define um único inimigo)
|    |-- 📄 EnemyType.cs    (Define os "tipos" de inimigos e suas pontuações)
|    |-- 📄 GameObject.cs   (Classe base para todos os objetos do jogo)
|    |-- 📄 Player.cs       (Controla o estado do jogador)
|
|-- 📁 Services/
|    |-- 📄 EnemyManager.cs   (Gerencia a horda de inimigos)
|    |-- 📄 HighScoreManager.cs (Gerencia a leitura e escrita dos placares)
|    |-- 📄 SoundManager.cs   (Gerencia o carregamento e reprodução dos sons)
|
|-- 📁 Sounds/
|    |-- (Seus arquivos de áudio .wav e .mp3)
|
|-- 📄 MainPage.xaml       (A interface do jogo)
|-- 📄 MainPage.xaml.cs    (O "maestro" que coordena as classes de lógica)
🛠️ Como Executar o Projeto
Pré-requisitos:

SDK do .NET 8 (ou superior).

Um ambiente de desenvolvimento como Visual Studio 2022 ou JetBrains Rider.

Passos:

Clone ou baixe este repositório.

Abra o arquivo da solução (.sln) no seu ambiente de desenvolvimento.

O ambiente irá restaurar automaticamente os pacotes NuGet necessários (como o NAudio).

Selecione o projeto de inicialização (ex: SpaceInvaders.Skia.Wpf para rodar no Windows).

Execute o projeto em modo de depuração (Debug).

🎮 Como Jogar
Use as Setas Direita/Esquerda ou as teclas A / D para mover a nave.

Pressione a Barra de Espaço para atirar.

Destrua os aliens para marcar pontos e proteja-se atrás dos escudos!

Autor:

Diogo Santos Cruz
