using System;

namespace Anatawa12.AvatarOptimizer
{
    public static class AnimatorWeightChanges
    {
        public static AnimatorWeightChange ForDurationAndWeight(float duration, float weight) =>
            duration != 0 ? AnimatorWeightChange.Variable : ForWeight(weight);

        public static AnimatorWeightChange ForWeight(float weight)
        {
            switch (weight)
            {
                case 0:
                    return AnimatorWeightChange.AlwaysZero;
                case 1:
                    return AnimatorWeightChange.AlwaysOne;
                default:
                    return AnimatorWeightChange.Variable;
            }
        }
        
        public static AnimatorWeightChange Merge(this AnimatorWeightChange a, AnimatorWeightChange b)
        {
            // 25 pattern
            if (a == b) return a;

            if (a == AnimatorWeightChange.NotChanged) return b;
            if (b == AnimatorWeightChange.NotChanged) return a;

            if (a == AnimatorWeightChange.Variable) return AnimatorWeightChange.Variable;
            if (b == AnimatorWeightChange.Variable) return AnimatorWeightChange.Variable;

            if (a == AnimatorWeightChange.AlwaysOne && b == AnimatorWeightChange.AlwaysZero)
                return AnimatorWeightChange.EitherZeroOrOne;
            if (b == AnimatorWeightChange.AlwaysOne && a == AnimatorWeightChange.AlwaysZero)
                return AnimatorWeightChange.EitherZeroOrOne;

            if (a == AnimatorWeightChange.EitherZeroOrOne && b == AnimatorWeightChange.AlwaysZero)
                return AnimatorWeightChange.EitherZeroOrOne;
            if (b == AnimatorWeightChange.EitherZeroOrOne && a == AnimatorWeightChange.AlwaysZero)
                return AnimatorWeightChange.EitherZeroOrOne;

            if (a == AnimatorWeightChange.EitherZeroOrOne && b == AnimatorWeightChange.AlwaysOne)
                return AnimatorWeightChange.EitherZeroOrOne;
            if (b == AnimatorWeightChange.EitherZeroOrOne && a == AnimatorWeightChange.AlwaysOne)
                return AnimatorWeightChange.EitherZeroOrOne;

            throw new ArgumentOutOfRangeException();
        }
    }

    public class AnimatorWeightChangesList
    {
        private readonly AnimatorWeightChange[] _changes;

        public AnimatorWeightChangesList(int layerCount)
        {
            _changes = new AnimatorWeightChange[layerCount];
        }

        public AnimatorWeightChange this[int index]
        {
            get => 0 <= index && index < _changes.Length ? _changes[index] : AnimatorWeightChange.NotChanged;
            set
            {
                if (index < 0 || index >= _changes.Length) return;
                _changes[index] = value;
            }
        }

        public AnimatorWeightChange Get(int i) => this[i];
    }

    public enum AnimatorWeightChange
    {
        NotChanged,
        AlwaysZero,
        AlwaysOne,
        EitherZeroOrOne,
        Variable
    }
}
