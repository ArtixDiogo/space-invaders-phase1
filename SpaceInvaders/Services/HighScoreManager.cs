namespace SpaceInvaders.Services
{
    public class HighScoreManager
    {
        private const string FileName = "highscores.txt";

        // Método para adicionar uma nova pontuação com apelido
        public async Task AddScoreAsync(string nickname, int score)
        {
            var scores = await GetScoresAsync();
            scores[nickname] = scores.ContainsKey(nickname) ? Math.Max(scores[nickname], score) : score;
            
            var topScores = scores.OrderByDescending(s => s.Value).Take(10);

            var lines = topScores.Select(s => $"{s.Key}:{s.Value}");

            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteLinesAsync(file, lines);
        }

        // Método para ler as pontuações do arquivo
        public async Task<Dictionary<string, int>> GetScoresAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(FileName);
                var lines = await FileIO.ReadLinesAsync(file);

                return lines.Select(line => line.Split(':'))
                    .Where(parts => parts.Length == 2 && int.TryParse(parts[1], out _))
                    .ToDictionary(parts => parts[0], parts => int.Parse(parts[1]));
            }
            catch (System.IO.FileNotFoundException)
            {
                return new Dictionary<string, int>();
            }
        }
    }
}
