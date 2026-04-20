using System;
using System.Collections.Generic;
using System.Text;

namespace MyAgentFramework.Tools
{
    internal class ReciprocalRankFusion
    {
        public class ScoredItem
        {
            public int Id { get; set; }
            public double RRFScore { get; set; }
        }

        public List<int> CombineRRF(List<int> vectorIds, List<int> keywordIds, int take = 5, int k = 60)
        {
            var scoreBoard = new Dictionary<int, double>();

            // 處理向量搜尋排名
            for (int i = 0; i < vectorIds.Count; i++)
            {
                int id = vectorIds[i];
                int rank = i + 1;
                scoreBoard[id] = scoreBoard.GetValueOrDefault(id, 0) + (1.0 / (k + rank));
            }

            // 處理關鍵字搜尋排名
            for (int i = 0; i < keywordIds.Count; i++)
            {
                int id = keywordIds[i];
                int rank = i + 1;
                scoreBoard[id] = scoreBoard.GetValueOrDefault(id, 0) + (1.0 / (k + rank));
            }

            // 根據 RRF 分數降冪排列，取前 5 名
            return scoreBoard.OrderByDescending(x => x.Value)
               .Select(x => x.Key)
               .Take(take)
               .ToList();
        }
    }
}
