# GZipCompressor

A console utility for compressing and restoring (decompressing) files using the GZip method (DEFLATE algorithm).

User instructions:
1. File compression: GZipCompressor.exe compress [file name] [archive name]
2. File restoration: GZipCompressor.exe decompress [archive name] [file name]
3. Cancel operation: Ctrl+C

File compression algorithm:

1. The source file is divided into data blocks of a specified size (10 MB by default).
2. A thread is created for each block. The maximum number of threads is limited to the specified number (5 by default).
3. In separate threads, the program reads blocks from the source file and compresses them.
4. In separate threads, compressed blocks are entered into a queue for writing to the archive. The maximum number of blocks in the queue for writing is limited to the specified number (10 by default).
5. In a separate thread, compressed blocks are sequentially extracted from the queue and written to the archive file.

File recovery (decompression) algorithm:

1. Compressed data blocks are sequentially read from the archive. The beginning of a compressed block is determined using the standard GZIP header (https://www.ietf.org/rfc/rfc1952.txt).
2. A thread is created for each compressed block. The maximum number of threads is limited to the specified number (5 by default).
3. In separate threads, the program reads blocks from the archive and decompresses them. To support large data blocks, blocks are read and decompressed in parts (subblocks). The default part size is 10 MB.
4. In separate threads, decompressed blocks are entered into a queue for writing to the file. The maximum number of blocks in the write queue is limited to the specified number (10 by default).
5. In a separate thread, decompressed blocks are sequentially extracted from the queue and are written to the file.

Консольная утилита для сжатия и восстановления (декомпрессии) файлов, использующая метод GZip (алгоритм DEFLATE).

Инструкция по использованию:  
1. Сжатие файла: GZipCompressor.exe compress [имя файла] [имя архива]  
2. Восстановление файла: GZipCompressor.exe decompress [имя архива] [имя файла]  
3. Отмена операции: Ctrl+C  

Алгоритм сжатия файла:  
1. Исходный файл разбивается на блоки данных заданного размера (по умолчанию 10 мб).  
2. Для каждого блока создается поток. Максимальное количество потоков ограничивается указанным числом (по умолчанию 5).  
3. В отдельных потоках программа считывает блоки из исходного файла и сжимает их.  
4. В отдельных потоках сжатые блоки заносятся в очередь на запись в архив. Максимальное количество блоков в очереди на запись ограничивается указанным числом (по умолчанию 10).  
5. В отдельном потоке сжатые блоки последовательно извлекаются из очереди и записываются в файл архива.  

Алгоритм восстановления (декомпресии) файла:  
1. Выполняется последовательное считывание сжатых блоков данных из архива. Начало сжатого блока определяется с помощью стандартного заголовка формата GZIP (https://www.ietf.org/rfc/rfc1952.txt).  
2. Для каждого сжатого блока создается поток. Максимальное количество потоков ограничивается указанным числом (по умолчанию 5).  
3. В отдельных потоках программа считывает блоки из архива и распаковывает их. Для поддержки больших блоков данных блоки считываются и распаковываются по частям (подблокам). Размер части по умолчанию: 10 мб.  
4. В отдельных потоках распакованные блоки заносятся в очередь на запись в файл. Максимальное количество блоков в очереди на запись ограничивается указанным числом (по умолчанию 10).  
5. В отдельном потоке распакованные блоки последовательно извлекаются из очереди и записываются в файл.  
