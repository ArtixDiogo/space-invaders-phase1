namespace SpaceInvaders.Services
{
    public class HighScoreManager
    {
        private const string FileName = "highscores.txt";

        // Método para adicionar uma nova pontuação
        public async Task AddScoreAsync(int score)
        {
            // Pega a lista de scores atuais
            var scores = await GetScoresAsync();
            // Adiciona o novo score
            scores.Add(score);
            
            // Ordena do maior para o menor e pega apenas os 10 melhores
            var topScores = scores.OrderByDescending(s => s).Take(10).ToList();

            // Converte os números para texto para salvar no arquivo
            var lines = topScores.Select(s => s.ToString());

            // Salva no arquivo
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteLinesAsync(file, lines);
        }

        // Método para ler as pontuações do arquivo
        public async Task<List<int>> GetScoresAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(FileName);
                var lines = await FileIO.ReadLinesAsync(file);
                // Converte o texto lido de volta para números
                return lines.Select(int.Parse).ToList();
            }
            catch (System.IO.FileNotFoundException)
            {
                // Se o arquivo não existe, retorna uma lista vazia
                return new List<int>();
            }
        }
    }
}
