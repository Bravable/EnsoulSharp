using System;
using EnsoulSharp.SDK;

namespace Overwrite
{
    public class NewPrediction : IPrediction
    {
        public PredictionOutput GetPrediction(PredictionInput input)
        {
            throw new NotImplementedException();
        }

        public PredictionOutput GetPrediction(PredictionInput input, bool ft, bool checkCollision)
        {
            throw new NotImplementedException();
        }
    }
}
