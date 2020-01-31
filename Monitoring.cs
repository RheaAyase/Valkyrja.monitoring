using System;
using Prometheus;

namespace Valkyrja.monitoring
{
	public class Monitoring: IDisposable
	{
		private readonly MetricPusher Prometheus;

		public readonly Gauge LatencyRedbox = Metrics.CreateGauge("hw_net_latency_redbox", "Server: Network latency between our servers");

		public Monitoring(Config config)
		{
			if( this.Prometheus == null )
				this.Prometheus = new MetricPusher(config.PrometheusEndpoint, config.PrometheusJob, intervalMilliseconds:(long)(1f / config.TargetFps * 1000));
			this.Prometheus.Start();
		}

		public void Dispose()
		{
			this.Prometheus.Stop();
			((IDisposable)this.Prometheus)?.Dispose();
		}
	}
}
