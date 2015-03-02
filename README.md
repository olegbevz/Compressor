# GZipCompressor
Консольная утилита для сжатия и восстановления (декомпрессии) файлов, использующая метод GZip (алгоритм DEFLATE).

Инструкция по использованию.
1. Сжатие файла: GZipCompressor.exe compress [имя файла] [имя архива]
2. Восстановление файла: GZipCompressor.exe decompress [имя архива] [имя файла]
3. Отмена операции: Ctrl+C

Алгоритм сжатия файла:
1. Исходный файл разбивается на блоки данных заданного размера (по умолчанию 10 мб).
2. Для каждого блока создается поток. Максимальное количество потоков ограничивается указанным числом (по умолчанию 5).
3. В рамках каждого потока программа выполняет считывание из исходного блока и сжатие блока данных. Затем блок данных заносится в очередь на запись в архив.
4. В отдельном потоке сжатые блоки данных последовательно записываются в файл архива.

Алгоритм восстановления (декомпресии) файла:
1. Выполняется последовательное считывание сжатых блоков данных из архива. Начало сжатого блока определяется с помощью стандартного заголовка формата GZIP (https://www.ietf.org/rfc/rfc1952.txt).
2. Для каждого сжатого блока данных создается поток. Максимальное количество потоков ограничивается указанным числом (по умолчанию 5).
3. В рамках каждого потока программа выполняет считывание из архива и восстановление блока данных. Затем блок данных заносится в очередь на запись в распакованный файл.
4. В отдельном потоке восстановленные блоки данных последовательно записываются в распакованный файл.
