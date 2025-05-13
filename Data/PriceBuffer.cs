namespace volatility_service
{
    public class PriceBuffer 
    {
        private decimal[] _buffer;
        private int _ix;
        private int _capacity;

        public PriceBuffer(int capacity)
        {
            _buffer = new decimal[capacity];
            _ix = 0;            
            _capacity = capacity;     
        }

        public int Ix => _ix;

        public void Add(decimal item) 
        {
            _buffer[_ix] = item;
            _ix = (_ix + 1) % _capacity;
        }

        public decimal AvgarageOfLast(int count)
        {
            decimal sum = 0;
            int arrIndex = 0;
            int remainder = Math.Abs(Math.Min(_ix - count, 0));

            for (int i = _ix - 1; i >= 0; i--)
            {                
                sum += _buffer[i];
                arrIndex++;
            }

            for (int i = _capacity - 1; i > _capacity - 1 - remainder; i--)
            {
                sum += _buffer[i];
                arrIndex++;
            }

            return sum/count;
        }

        public decimal[] TakeLast(int count)
        {
            var arr = new decimal[count];
            int arrIndex = 0;
            int remainder = Math.Abs(Math.Min(_ix - count, 0));

            for (int i = _ix - 1; i >= 0; i--)
            {                
                arr[arrIndex] = _buffer[i];
                arrIndex++;
            }

            for (int i = _capacity - 1; i > _capacity - 1 - remainder; i--)
            {
                arr[arrIndex] = _buffer[i];
                arrIndex++;
            }

            return arr;
        }
    }

    
}