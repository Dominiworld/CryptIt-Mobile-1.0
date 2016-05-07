/*
* ������� �.�.
* ����� ��� ������ � ������.
*/

using System;
using System.Collections;

namespace CryptingTool
{
    /// <summary>
    /// ����� ��� ������ � ������.
    /// ���� �������� � ������� ���� ArrayList, ��� ���������� ������ � ����.
    /// </summary>
    class Bits
    {
        private ArrayList bits = new ArrayList();

        private int len = 0, num = 0; 

        /// <summary>
        /// �����������. ��������� ����� �������� � �������� ���.
        /// </summary>
        public Bits(int value)
        {
            num = value; ToBits();
        }

        /// <summary>
        /// �����������. ��� ������� ����������� � �������� ���.
        /// </summary>
        public Bits(char value)
        {
            num = (int)value; ToBits();
        }

        /// <summary>
        /// �����������. ��� ������� ����������� � �������� ���.
        /// </summary>
        public Bits(byte value)
        {
            num = (int)value; ToBits();
        }

        /// <summary>
        /// �����������. ����������� ������ � "������� ������"
        /// ��� ��������� �������� ������ ���������������� ��� '1'.
        /// </summary>
        public Bits(string value)
        {
            len = value.Length;

            for (int i = 0; i < len; i++)
                if (value[i] == '0') bits.Add(0);
                else bits.Add(1);
        }
        
        /// <summary>
        /// ��������. ���������� ������� ���������� ���.
        /// </summary>
        public int Length
        {
            get
            {
                return len;
            }
        }

        /// <summary>
        /// ��������. ���������� ���������� ������������� "�������� �������".
        /// </summary>
        public int Number
        {
            get
            {
                ToInt(); return num;
            }
        }

        /// <summary>
        /// ��������. ���������� ������, ������� ����������� ������� ���������� ������.
        /// </summary>
        public char Char
        {
            get
            {
                ToInt(); return (char)num;
            }
        }

        /// <summary>
        /// �������� ���������� []. ���������� i-�� ������� "�������� �������".
        /// </summary>
        public int this[int i]
        {
            get
            {
                if (i >= 0 && i < len) return (int)bits[i];
                else return -1;
            }

            set
            {
                if (i >= 0 && i < len)
                    if (value > 0) bits[i] = 1;
                    else bits[i] = 0;
            }
        }

        /// <summary>
        /// ������� ��������� � ����� ����� �������.
        /// ��� ��������� �������� ���������������� ��� '1'.
        /// </summary>
        public void Add(object value)
        {
            if ((int)value == 0) bits.Add(0);
            else bits.Add(1);
            
            len++;
        }

        /// <summary>
        /// ������� ��������� � ������� index �������� value
        /// ��� ��������� �������� ���������������� ��� '1'.
        /// </summary>
        public void Insert(int index, object value)
        {
            if (index < 0 || index > len) return;

            if (value.Equals(0)) bits.Insert(index, 0);
            else bits.Insert(index, 1);

            len++;
        }

        /// <summary>
        /// ������� ������� �������� �� ������� index.
        /// </summary>
        public void Erase(int index)
        {
            if (index < 0 || index > len) return;

            bits.RemoveAt(index); len--;
        }

        /// <summary>
        /// ������� ����������� ��� ����. '0' = '1'; '1' = '0'.
        /// </summary>
        public void InvertBits()
        {
            for (int i = 0; i < len; i++)
                if (bits[i].Equals(0)) bits[i] = 1;
                else bits[i] = 0;
        }

        /// <summary>
        /// ������� ������������� "������� ������" ����� �������.
        /// </summary>
        public void Reverse()
        {
            bits.Reverse();
        }

        /// <summary>
        /// ������� ����������� ������� "������� ������" � ������.
        /// </summary>
        public string ToString()
        {
            string str = "";

            for (int i = 0; i < len; i++)
                str += bits[i].ToString();

            return str;
        }

        /// <summary>
        /// ������� ���������� ����������� ����� "�������� �������" �����.
        /// </summary>
        public void ToLeft()
        {
            object tmp;

            for (int i = 1; i < len - 1; i++)
            {
                tmp = bits[i + 1]; bits[i + 1] = bits[i]; bits[i] = tmp;
            }

            tmp = bits[0]; bits[0] = bits[len - 1]; bits[len - 1] = tmp;
        }

        /// <summary>
        /// ������� ���������� ����������� ����� "�������� �������" ������.
        /// </summary>
        public void ToRight()
        {
            object tmp;

            tmp = bits[0]; bits[0] = bits[len - 1]; bits[len - 1] = tmp;

            for (int i = len - 2; i != 0; i--)
            {
                tmp = bits[i]; bits[i] = bits[i + 1]; bits[i + 1] = tmp;
            }
        }

        /// <summary>
        /// ������� ���������� ������� ����������� ����� � �������� �������������.
        /// </summary>
        private void ToBits()
        {
            int temp = num;

            while (temp != 0)
            {
                bits.Add(temp % 2); temp /= 2;
            }

            len = bits.Count; bits.Reverse();
        }

        /// <summary>
        /// ������� ���������� ������� �� ��������� ������������� � ����������.
        /// </summary>
        private void ToInt()
        {
            num = 0;

            for (int i = 0; i < len; i++)
                if (bits[len - i - 1].Equals(1))
                    num += (int)Math.Pow(2, i);
        }
    }
}