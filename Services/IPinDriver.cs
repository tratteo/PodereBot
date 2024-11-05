internal interface IPinDriver
{
    Task PinHigh(int? pin);
    Task PinLow(int? pin);
}
