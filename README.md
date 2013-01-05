osm-bulk-tools
==============

tools for bulk import/export data to/from OpenStreetMap

written in 2012 by Dmitry Gurovich (yrtimid@gmail.com)

Originally was developed to perform massive street names fixes.

The process was as follows:
* Filter needed data into filtered_dump.osm (for example all streets within city)
* osmconvert filtered_dump.osm --csv-headline --csv="@id @lon @lat is_in highway name name:he name:he1 name:he2 name:he3 name:ar name:ru name:en name:en1 name:en2 name:en3 name:en4 name:en5 name:en6" -o=data.csv
* Upload data.csv to the google spreadsheet, edit and export back into CSV file.
* Use OsmBulkTools to upload changed data to the OSM.

Current version supports updating tags in existing OSM data from external csv file.

In other words: for each element in CSV (recognized by id and type) downloaded original data from the server, and then tag values from csv copied into.

All element tags which were not in CSV - untouched. If CSV have empty value for some tag - this tag is removed. If CSV have updated value - it will replace old value. If CSV have not existing tag - it will be added.

All columns which starts from "@" ignored.

Special parameters used to pass names of the "id" and "type" columns. (type recognized as in osmconvert 0-node, 1-way, 2-relation)

Another special column "update" may be used, to process only marked rows in CSV. Column may have any non empty value.


## License

osm-bulk-tools is available under the [WTFPL](http://sam.zoy.org/wtfpl/).

## Included projects 

* https://github.com/yrtimiD/osm-api-dotnet is licensed under the [WTFPL](http://sam.zoy.org/wtfpl/).

* A Fast CSV Reader By Sebastien Lorion, 10 Nov 2011 (http://www.codeproject.com/Articles/9258/A-Fast-CSV-Reader) Licensed under The MIT License

## Compiled binaries

https://www.dropbox.com/sh/evhxpi2xqgihm7i/ZcPDP9LBg3
