// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;



class LoadingView: IView
{
    public const int RedrawMilliseconds = 250;
    public LoadingProgress LoadingProgress = new();
    private string[] spinners = { "-", "\\", "|", "/" };

    public LoadingView(IView parent) : base(parent) { }

    private string GetCurrentSpinner() {
        long milliseconds = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % 1000;
        int idx = (int)milliseconds / RedrawMilliseconds;
        return spinners[idx];
    }

    public override void Draw(Console console) {
        var ctx = new DrawContext(this, console);

        string spinner = GetCurrentSpinner();
        ctx.DrawRow($"  {spinner} Looking for files...");
        ctx.DrawRow($"  Found:   {LoadingProgress.Processed}");
        ctx.DrawRow($"  Ignored: {LoadingProgress.Ignored}");

        while (ctx.usedRows < Size.height) {
            ctx.DrawRow("");
        }
    }

}

