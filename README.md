QUSMA Performance Analytics Suite (QPAS) [![Build status](https://ci.appveyor.com/api/projects/status/vkcttigbnhudcwkw/branch/master?svg=true)](https://ci.appveyor.com/project/qusma/qpas/branch/master)
====
QPAS is a trading journal designed for Interactive Brokers clients, featuring detailed performance, risk, and execution analytics. For a more detailed look into its features, check out the documentation.

* [Installer](https://qusma.com/QPAS/setup.exe)
* [Documentation](https://github.com/qusma/QPAS/wiki)

For bug reports, feature requests, etc. use either the GitHub issue tracker or [gitter chat](https://gitter.im/qusma/community).

Features
------------------------
* Detailed performance statistics, at the portfolio, strategy, and trade levels
* Ex-post risk analytics
* Benchmark and backtest comparisons
* Execution analytics
* Trade journal
* Automation through scripting

Screenshots
------------------------

| | | |
|:-------------------------:|:-------------------------:|:-------------------------:|
|<a href="https://qusma.com/images/monte_carlo.png"><img alt="Monte Carlo" src="https://qusma.com/images/thumbnails/monte_carlo.png"></a>  Monte Carlo |  <a href="https://qusma.com/images/performance_overview.png"><img alt="Performance Overview" src="https://qusma.com/images/thumbnails/performance_overview.png"></a> Performance Overview|<a href="https://qusma.com/images/expected_shortfall.png"><img alt="Expected Shortfall" src="https://qusma.com/images/thumbnails/expected_shortfall.png"></a> Expected Shortfall|
|<a href="https://qusma.com/images/rolling_risk_contribution.png"><img alt="Risk Contribution" src="https://qusma.com/images/thumbnails/rolling_risk_contribution.png"></a> Strategy Risk Contribution |  <a href="https://qusma.com/images/execution_report.png"><img alt="Execution Analysis" src="https://qusma.com/images/thumbnails/execution_report.png"></a> Execution Analysis |<a href="https://qusma.com/images/trade_overview.png"><img alt="Trade Overview" src="https://qusma.com/images/thumbnails/trade_overview.png"></a> Trade Overview|
|<a href="https://qusma.com/images/realized_volatility.png"><img alt="Realized Volatility" src="https://qusma.com/images/thumbnails/realized_volatility.png"></a> Realized Volatility  | <a href="https://qusma.com/images/trade_notes.png"><img alt="Trade Notes" src="https://qusma.com/images/thumbnails/trade_notes.png"></a> Trade Notes|<a href="https://qusma.com/images/trade_stats.png"><img alt="Trade Stats" src="https://qusma.com/images/thumbnails/trade_stats.png"></a> Trade Stats|



Requirements
------------------------
* [.NET Core 3.1 or higher](https://dotnet.microsoft.com/download/dotnet-core)
* [QUSMA Data Management System](https://github.com/qusma/QDMS) (Optional, needed only for some features: benchmarks, execution analysis, and charting)

Contributing
------------------------
Simply send a pull request with your changes. If you're looking for something to do, the issue tracker has several bugs and planned features.

While QPAS currently only supports statements from Interactive Brokers, it can easily be extended to support other brokers. See [here](https://github.com/qusma/QPAS/wiki/Implementing-a-Statement-Parser) for more details.