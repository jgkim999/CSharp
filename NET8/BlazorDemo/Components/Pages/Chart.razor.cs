using BlazorBootstrap;

using WebDemo.Domain.Models;

namespace BlazorDemo.Components.Pages
{
    public partial class Chart
    {
        public LineChart lineChart = new();
        private LineChartOptions lineChartOptions = default!;
        private ChartData chartData = default!;

        private int datasetsCount;
        private int labelsCount;

        private Random random = new();

        private List<VuTimeline> TimeLine = new();

        private int vu = 100;
        private int duration = 30;

        protected override async Task OnInitializedAsync()
        {   
            chartData = new ChartData { Labels = GetDefaultDataLabels(1), Datasets = GetDefaultDataSets(0) };
            lineChartOptions = new() { Responsive = true, Interaction = new Interaction { Mode = InteractionMode.Index } };
            await Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await lineChart.InitializeAsync(chartData, lineChartOptions);
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        private List<string> GetDefaultDataLabels(int numberOfLabels)
        {
            var labels = new List<string>();
            for (var index = 0; index < numberOfLabels; index++)
            {
                labels.Add(GetNextDataLabel());
            }

            return labels;
        }

        private List<IChartDataset> GetDefaultDataSets(int numberOfDatasets)
        {
            var c = ColorBuilder.CategoricalTwelveColors[datasetsCount].ToColor();
            var datasets = new List<IChartDataset>();
            var lineChartDataset = new LineChartDataset()
            {
                Label = $"Team {datasetsCount}",
                Data = new List<double>(){0},
                BackgroundColor = new List<string> { c.ToRgbString() },
                BorderColor = new List<string> { c.ToRgbString() },
                BorderWidth = new List<double> { 2 },
                HoverBorderWidth = new List<double> { 4 },
                PointBackgroundColor = new List<string> { c.ToRgbString() },
                PointRadius = new List<int> { 0 }, // hide points
                PointHoverRadius = new List<int> { 4 }
            };
            datasets.Add(lineChartDataset);
            return datasets;
        }

        private List<IChartDataset> GetDataSets()
        {
            var c = ColorBuilder.CategoricalTwelveColors[datasetsCount].ToColor();
            var datasets = new List<IChartDataset>();

            var lineChartDataset = new LineChartDataset()
            {
                Label = $"Team {datasetsCount}",
                Data = new List<double>() { 0 },
                BackgroundColor = new List<string> { c.ToRgbString() },
                BorderColor = new List<string> { c.ToRgbString() },
                BorderWidth = new List<double> { 2 },
                HoverBorderWidth = new List<double> { 4 },
                PointBackgroundColor = new List<string> { c.ToRgbString() },
                PointRadius = new List<int> { 0 }, // hide points
                PointHoverRadius = new List<int> { 4 }
            };

            datasets.Add(lineChartDataset);
            return datasets;
        }

        private string GetNextDataLabel()
        {
            labelsCount += 1;
            return $"{labelsCount * 30} min";
        }

        private List<double> GetRandomData()
        {
            var data = new List<double>();
            for (var index = 0; index < labelsCount; index++)
            {
                data.Add(random.Next(200));
            }

            return data;
        }

        private LineChartDataset GetRandomLineChartDataset()
        {
            var c = ColorBuilder.CategoricalTwelveColors[datasetsCount].ToColor();

            datasetsCount += 1;

            return new LineChartDataset
            {
                Label = $"Team {datasetsCount}",
                Data = GetRandomData(),
                BackgroundColor = new List<string> { c.ToRgbString() },
                BorderColor = new List<string> { c.ToRgbString() },
                BorderWidth = new List<double> { 2 },
                HoverBorderWidth = new List<double> { 4 },
                PointBackgroundColor = new List<string> { c.ToRgbString() },
                PointRadius = new List<int> { 0 }, // hide points
                PointHoverRadius = new List<int> { 4 }
            };
        }

        private async Task AddDataAsync()
        {
            if (chartData is null || chartData.Datasets is null)
                return;
            
            var timeline = new VuTimeline()
            {
                VuMax = vu,
                DurationMin = duration
            };
            TimeLine.Add(timeline);

            List<string> labels = [$"0m"];

            List<double> data = [0];

            int labelStep = 1;
            foreach (var item in TimeLine)
            {
                var step = item.DurationMin / 30;
                for (int i = 0; i < step; ++i)
                {
                    data.Add(item.VuMax);
                    var hour = labelStep * 30 / 60;
                    var minute = (labelStep * 30) - (hour * 60);
                    labels.Add($"{hour}:{minute}");
                    labelStep++;
                }
            }
            var datasets = new List<IChartDataset>
            {
                new LineChartDataset
                {
                    Label = "India",
                    Data = data,
                    BackgroundColor = new List<string> { "rgb(88, 80, 141)" },
                    BorderColor = new List<string> { "rgb(88, 80, 141)" },
                    BorderWidth = new List<double> { 2 },
                    HoverBorderWidth = new List<double> { 4 },
                    PointBackgroundColor = new List<string> { "rgb(88, 80, 141)" },
                    PointBorderColor = new List<string> { "rgb(88, 80, 141)" },
                    PointRadius = new List<int> { 0 }, // hide points
                    PointHoverRadius = new List<int> { 4 }
                }
            };
            chartData = new ChartData { Labels = labels, Datasets = datasets };
            await lineChart.UpdateAsync(chartData, lineChartOptions);
            //chartData = await lineChart.AddDataAsync(chartData, GetNextDataLabel(), data);
        }
    }
}
