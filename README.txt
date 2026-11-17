O gerador fica em Assets/Scripts/Generation e pode ser executado
pelo console (sem Unity) ou pela Unity.


REQUISITOS
- Console: .NET SDK 10 (https://dotnet.microsoft.com/download)
- Unity (opcional): Unity 6, versao 6000.6.7f1


CONSOLE
Abra o terminal na pasta raiz do projeto e use os comandos abaixo.

1) Gerar uma casa com semente sorteada:

   dotnet run -c Release --project tools/preview

2) Gerar a casa de uma semente especifica (9 digitos):

   dotnet run -c Release --project tools/preview -- 537958074

   A mesma semente gera sempre a mesma casa.
   Legenda da planta: SS sala, CC cozinha, QQ quarto,
   BB banheiro, HH corredor.

3) Rodar uma bateria de testes com 1000 casas:

   dotnet run -c Release --project tools/preview -- batch

4) Rodar uma bateria escolhendo a quantidade de casas:

   dotnet run -c Release --project tools/preview -- batch 10000

5) Rodar uma bateria com tamanho fixo de casa (em celulas de 0,5 m):

   dotnet run -c Release --project tools/preview -- batch 10000 26x20

Ao final da bateria, o resumo aparece no console e dois arquivos
sao salvos na pasta results:
- ..._casas.csv: uma linha por casa gerada
- ..._parametros.txt: parametros usados na bateria


UNITY
1) Abra a pasta do projeto pelo Unity Hub.
2) Abra a cena Assets/Scenes/SampleScene.
3) Clique em Play.

Teclas:
R - gera uma nova casa
T - mostra ou esconde o teto
P - salva um print da tela

Para ver uma semente especifica, selecione o objeto HouseBuilder,
marque Fixed Seed e informe a semente no campo Seed