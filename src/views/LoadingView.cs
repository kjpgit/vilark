// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;



class LoadingView: IView
{
    public const int RedrawMilliseconds = 250;
    public Notification CurrentData = new Notification();
    private string[] spinners = { "-", "\\", "|", "/" };

    private string GetCurrentSpinner() {
        long milliseconds = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % 1000;
        int idx = (int)milliseconds / RedrawMilliseconds;
        return spinners[idx];
    }

    public override void Draw(Console console) {
        var ctx = new DrawContext(this, console);
        var snapshot = CurrentData;

        string spinner = GetCurrentSpinner();
        ctx.DrawRow($"  {spinner} Looking for files...");
        ctx.DrawRow($"  Found:   {snapshot.Processed ?? 0}");
        ctx.DrawRow($"  Ignored: {snapshot.Ignored ?? 0}");

        while (ctx.usedRows < Size.height) {
            ctx.DrawRow("");
        }
    }

}

