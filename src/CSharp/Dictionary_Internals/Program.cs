Dictionary<char, int> freqMap = new Dictionary<char, int>();
string word = "The cat sleeps.";
for (int i = 0; i < word.Length; i++)
{
    if (freqMap.ContainsKey(word[i]))
    {
        freqMap[word[i]]++;
    }
    else
    {
        freqMap.Add(word[i], 1);
    }
}

Console.WriteLine("Frequency of each character:");
foreach (var item in freqMap)
{
    Console.WriteLine($"{item.Key}: {item.Value}");
}