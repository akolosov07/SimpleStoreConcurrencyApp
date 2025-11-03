using System.Text;
using ConcurrencyApp;

namespace ConcurrencyAppTests;

public class SimpleStoreConcurrencyTests
{
    [Fact]
    public async Task GetsAndDeletesShouldBeCorrect()
    {
        const int totalKeys = 1000; // количество уникальных ключей
        const int half = totalKeys / 2;

        var keys = Enumerable.Range(0, totalKeys).Select(i => $"key_{i}").ToArray();

        using var store = new SimpleStore();

        var setTasks = keys.Select(k =>
            Task.Run(() =>
            {
                var value = Encoding.UTF8.GetBytes(k);
                store.Set(k, value);
            })).ToArray();

        await Task.WhenAll(setTasks);

        // Проверяем, что setCount == totalKeys
        var statsAfterSets = store.GetStatistics();
        Assert.Equal(totalKeys, statsAfterSets.setCount);

        // Параллельно выполняем половину Get, половину Delete
        var tasks = new List<Task>();

        for (int i = 0; i < half; i++)
        {
            var key = keys[i];
            tasks.Add(Task.Run(() =>
            {
                var v = store.Get(key);
            }));
        }

        for (int i = half; i < totalKeys; i++)
        {
            var key = keys[i];
            tasks.Add(Task.Run(() =>
            {
                store.Delete(key);
            }));
        }

        await Task.WhenAll(tasks);

        // Ожидаемые счётчики:
        // setCount == totalKeys
        // getCount == half (мы вызвали Get half раз)
        // deleteCount == half (удаляли существующие ключи — должно удалиться)
        var finalStats = store.GetStatistics();
        Assert.Equal(totalKeys, finalStats.setCount);
        Assert.Equal(half, finalStats.getCount);
        Assert.Equal(half, finalStats.deleteCount);

        // Проверка содержимого: должно остаться totalKeys - half ключей
        int remaining = 0;
        for (int i = 0; i < totalKeys; i++)
        {
            var v = store.Get(keys[i]);
            if (v != null) remaining++;
        }

        Assert.Equal(totalKeys - half, remaining);

        // Дополнительная проверка: значения для оставшихся ключей корректны
        for (int i = 0; i < half; i++)
        {
            var expected = Encoding.UTF8.GetBytes(keys[i]);
            var actual = store.Get(keys[i]);
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }
    }
}

