namespace Eco.Moose.Data
{
    public struct Either<T1, T2, T3, T4> where T1 : class where T2 : class where T3 : class where T4 : class
    {
        private readonly object _value;

        public Either(object value1)
        {
            _value = value1;
        }

        public TG Get<TG>() where TG : class
        {
            return _value as TG;
        }

        public readonly bool Is<TG>()
        {
            return _value is TG;
        }

        public readonly override bool Equals(object other)
        {
            return other is Either<T1, T2, T3, T4> either && either._value == _value;
        }

        public readonly bool Equals(Either<T1, T2, T3, T4> other)
        {
            return Equals(_value, other._value);
        }

        public readonly override int GetHashCode()
        {
            return _value != null ? _value.GetHashCode() : 0;
        }
    }
}
