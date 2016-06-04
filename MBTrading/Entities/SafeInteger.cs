namespace MBTrading
{
    public class SafeInteger
    {
        int value = 0;

        public SafeInteger(int value)
        {
            this.value = value;
        }

        public static implicit operator SafeInteger(int value)
        {
            return new SafeInteger(value);
        }

        public static implicit operator int(SafeInteger integer)
        {
            return integer.value;
        }

        public static int operator +(SafeInteger one, SafeInteger two)
        {
            return one.value + two.value;
        }

        public static SafeInteger operator +(int one, SafeInteger two)
        {
            return new SafeInteger(one + two);
        }

        public static int operator -(SafeInteger one, SafeInteger two)
        {
            return one.value - two.value;
        }

        public static SafeInteger operator -(int one, SafeInteger two)
        {
            return new SafeInteger(one - two);
        }
    }
}