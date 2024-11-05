using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

class ImageCompressor
{
    const int width = 320;
    const int height = 200;
    const int blockSize = 4;

    static void Main()
    {
        Console.InputEncoding = Encoding.Unicode;
        Console.OutputEncoding = Encoding.Unicode;

        // Зчитування початкового зображення в масив
        string inputImagePath = @"C:\Users\yaros\Downloads\laba5.bmp"; // Змініть на свій шлях
        byte[,] imageArray = LoadImageToArray(inputImagePath);

        // Запис початкового зображення в файл
        string uncompressedFilePath = @"C:\Users\yaros\Downloads\uncompressed_image.bin";
        SaveArrayToFile(imageArray, uncompressedFilePath);

        // Розклад на блоки 4x4
        double[,] dctCoefficients = PerformDCT(imageArray);
        int[,] quantizationMatrix = GetQuantizationMatrix();
        double[,] quantizedDCT = QuantizeDCT(dctCoefficients, quantizationMatrix);

        // Запис DCT компонентів в файл
        string dctFilePath = @"C:\Users\yaros\Downloads\dct_components.bin";
        SaveDCTToFile(quantizedDCT, dctFilePath);

        // Відновлення зображення
        byte[,] restoredImage = PerformInverseDCT(quantizedDCT);
        string restoredImagePath = @"C:\Users\yaros\Downloads\restored_image.bmp";
        SaveArrayToImage(restoredImage, restoredImagePath);

        // Відкриття зображення для перегляду
        OpenImage(restoredImagePath);

        // Порівняльне дослідження
        double mseRestored = CalculateMSE(imageArray, restoredImage);
        Console.WriteLine($"Середньоквадратичне відхилення між оригінальним і відновленим зображенням: {mseRestored}");

        // Обчислення MSE між оригінальним зображенням і DCT компонентами
        byte[,] dctImage = ConvertDctToByteArray(quantizedDCT);
        double mseDct = CalculateMSE(imageArray, dctImage);
        Console.WriteLine($"Середньоквадратичне відхилення між оригінальним зображенням і DCT компонентами: {mseDct}");
    }

    static byte[,] LoadImageToArray(string filename)
    {
        Bitmap bmp = new Bitmap(filename);
        byte[,] data = new byte[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = bmp.GetPixel(x, y);
                data[y, x] = (byte)((pixel.R + pixel.G + pixel.B) / 3); // Сірий рівень
            }
        }

        return data;
    }

    static void SaveArrayToFile(byte[,] data, string filename)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    writer.Write(data[y, x]);
                }
            }
        }
    }

    static double[,] PerformDCT(byte[,] image)
    {
        double[,] dctCoefficients = new double[height, width];
        for (int y = 0; y < height; y += blockSize)
        {
            for (int x = 0; x < width; x += blockSize)
            {
                double[,] block = new double[blockSize, blockSize];
                for (int j = 0; j < blockSize; j++)
                    for (int i = 0; i < blockSize; i++)
                        block[j, i] = image[y + j, x + i];

                double[,] dctBlock = ComputeDCT(block);
                for (int j = 0; j < blockSize; j++)
                    for (int i = 0; i < blockSize; i++)
                        dctCoefficients[y + j, x + i] = dctBlock[j, i];
            }
        }
        return dctCoefficients;
    }

    static double[,] QuantizeDCT(double[,] dct, int[,] quantizationMatrix)
    {
        double[,] quantizedDCT = new double[height, width];
        for (int y = 0; y < height; y += blockSize)
        {
            for (int x = 0; x < width; x += blockSize)
            {
                for (int j = 0; j < blockSize; j++)
                    for (int i = 0; i < blockSize; i++)
                        quantizedDCT[y + j, x + i] = Math.Round(dct[y + j, x + i] / quantizationMatrix[j, i]);
            }
        }
        return quantizedDCT;
    }

    static byte[,] PerformInverseDCT(double[,] quantizedDCT)
    {
        byte[,] restoredImage = new byte[height, width];
        for (int y = 0; y < height; y += blockSize)
        {
            for (int x = 0; x < width; x += blockSize)
            {
                double[,] block = new double[blockSize, blockSize];
                for (int j = 0; j < blockSize; j++)
                    for (int i = 0; i < blockSize; i++)
                        block[j, i] = quantizedDCT[y + j, x + i];

                double[,] restoredBlock = ComputeInverseDCT(block);
                for (int j = 0; j < blockSize; j++)
                    for (int i = 0; i < blockSize; i++)
                        restoredImage[y + j, x + i] = (byte)Math.Clamp(Math.Round(restoredBlock[j, i]), 0, 255);
            }
        }
        return restoredImage;
    }

    static int[,] GetQuantizationMatrix()
    {
        int[,] matrix = new int[blockSize, blockSize];
        Console.WriteLine("Введіть коефіцієнти квантування для кожного з 16-ти елементів:");
        for (int i = 0; i < blockSize; i++)
            for (int j = 0; j < blockSize; j++)
            {
                Console.Write($"Коефіцієнт для позиції ({i + 1},{j + 1}): ");
                matrix[i, j] = int.Parse(Console.ReadLine());
            }
        return matrix;
    }

    static double[,] ComputeDCT(double[,] block)
    {
        double[,] result = new double[blockSize, blockSize];
        double coef = Math.PI / (2.0 * blockSize);

        for (int u = 0; u < blockSize; u++)
        {
            for (int v = 0; v < blockSize; v++)
            {
                double sum = 0.0;
                for (int i = 0; i < blockSize; i++)
                {
                    for (int j = 0; j < blockSize; j++)
                    {
                        sum += block[i, j] * Math.Cos((2 * i + 1) * u * coef) * Math.Cos((2 * j + 1) * v * coef);
                    }
                }
                result[u, v] = sum * Alpha(u) * Alpha(v) / 4.0;
            }
        }
        return result;
    }

    static double Alpha(int x) => x == 0 ? 1 / Math.Sqrt(2) : 1.0;

    static double[,] ComputeInverseDCT(double[,] block)
    {
        double[,] result = new double[blockSize, blockSize];
        double coef = Math.PI / (2.0 * blockSize);

        for (int i = 0; i < blockSize; i++)
        {
            for (int j = 0; j < blockSize; j++)
            {
                double sum = 0.0;
                for (int u = 0; u < blockSize; u++)
                {
                    for (int v = 0; v < blockSize; v++)
                    {
                        sum += Alpha(u) * Alpha(v) * block[u, v] * Math.Cos((2 * i + 1) * u * coef) * Math.Cos((2 * j + 1) * v * coef);
                    }
                }
                result[i, j] = sum / 4.0;
            }
        }
        return result;
    }

    static void SaveDCTToFile(double[,] dct, string filename)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    writer.Write(dct[y, x]);
                }
            }
        }
    }

    static void SaveArrayToImage(byte[,] image, string filename)
    {
        Bitmap bmp = new Bitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte pixel = image[y, x];
                bmp.SetPixel(x, y, Color.FromArgb(pixel, pixel, pixel)); // Грейскейл
            }
        }
        bmp.Save(filename, ImageFormat.Bmp);
    }

    static void OpenImage(string filename)
    {
        Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
    }

    static double CalculateMSE(byte[,] original, byte[,] comparison)
    {
        double mse = 0.0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                mse += Math.Pow(original[y, x] - comparison[y, x], 2);
            }
        }
        return mse / (width * height);
    }

    static byte[,] ConvertDctToByteArray(double[,] dct)
    {
        byte[,] dctImage = new byte[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                dctImage[y, x] = (byte)Math.Clamp(Math.Round(dct[y, x]), 0, 255);
            }
        }
        return dctImage;
    }
}