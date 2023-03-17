# StockBridge Backend
This project is using .Net EF Core 7.0 with CefSharp.OffScreen.NETCore to go cars.com and do several different jobs in there:
1-Login web site with credentials,
2-Select some filters and search with that filters,
3-Read two result page, first and second one,
4-Choose random car and gather its full data,
5-Click Home Delivery in that car's detail page (if that car does not have Home Delivery Badge, code will recursively check different cars)
6-Go back to result page, filter extra
7-Do 3,4,5 steps again for new results
8-Export result as json to desktop

Here is the [example JSON file output](Docs/Result.json) for this project.
