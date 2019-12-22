

function getCorrelation(xArray, yArray) {
    function sum(m, v) {return m + v;}
    function sumSquares(m, v) {return m + v * v;}
    function filterNaN(m, v, i) {isNaN(v) ? null : m.push(i); return m;}
  
    // clean the data (because we know that some values are missing)
    var xNaN = _.reduce(xArray, filterNaN , []);
    var yNaN = _.reduce(yArray, filterNaN , []);
    var include = _.intersection(xNaN, yNaN);
    var fX = _.map(include, function(d) {return xArray[d];});
    var fY = _.map(include, function(d) {return yArray[d];});
  
    var sumX = _.reduce(fX, sum, 0);
    var sumY = _.reduce(fY, sum, 0);
    var sumX2 = _.reduce(fX, sumSquares, 0);
    var sumY2 = _.reduce(fY, sumSquares, 0);
    var sumXY = _.reduce(fX, function(m, v, i) {return m + v * fY[i];}, 0);
  
    var n = fX.length;
    var ntor = ( ( sumXY ) - ( sumX * sumY / n) );
    var dtorX = sumX2 - ( sumX * sumX / n);
    var dtorY = sumY2 - ( sumY * sumY / n);
   
    var r = ntor / (Math.sqrt( dtorX * dtorY )); // Pearson ( http://www.stat.wmich.edu/s216/book/node122.html )
    var m = ntor / dtorX; // y = mx + b
    var b = ( sumY - m * sumX ) / n;
  
    // console.log(r, m, b);
    return {r: r, m: m, b: b};
  }

// SVG AND D3 STUFF
var svg = d3.select("#chart")
    .append("svg")
    .attr("width", 1000)
    .attr("height", 640);

svg.append('g')
    .classed('chart', true)
    .attr('transform', 'translate(80, -60)');

// Country name
let lbl2 = d3.select('svg g.chart').append('text');
lbl2.attr( 'id', 'countryLabel')
lbl2.attr('x', 0);
lbl2.attr('y', 170 );
lbl2.style({ 'font-size': '80px', 'font-weight': 'bold', 'fill': '#ddd' });


// Best fit line (to appear behind points)
d3.select('svg g.chart')
    .append('line')
    .attr('id', 'bestfit');


// Axis labels
let lbl = d3.select('svg g.chart').append('text');
lbl.attr( 'id', 'xLabel')
lbl.attr( 'x', 400);
lbl.attr( 'y', 670);
lbl.attr('text-anchor', 'middle' );
lbl.text('hello moto');;

lbl = d3.select('svg g.chart').append('text');
lbl.attr('transform', 'translate(-60, 330)rotate(-90)');
lbl.attr( 'id', 'yLabel');
lbl.attr('text-anchor', 'middle' );
//lbl.text('Well-being (scale of 0-10)');




// Render axes
d3.select('svg g.chart')
    .append("g")
    .attr('transform', 'translate(0, 630)')
    .attr('id', 'xAxis');

d3.select('svg g.chart')
    .append("g")
    .attr('id', 'yAxis')
    .attr('transform', 'translate(-10, 0)');
    
    


//// RENDERING FUNCTIONS
function updateChart(data) {


    const minP = _.min(data, (x) => x.y).y;
    const maxP = _.max(data, (x) => x.y).y;

    const yScale = d3.scaleLinear()
        .domain([maxP, minP])
        .range([100, 600]);

    console.log('min price', minP, 'max price', maxP);

    const minA = _.min(data, (x) => x.x).x;
    const maxA = _.max(data, (x) => x.x).x;

    const xScale = d3.scaleLinear()
            .domain([minA, maxA])
            .range([20, 780]);


    const countryLabel = d3.select('#data');


    d3.select('svg g.chart')
        .selectAll('circle')
        .remove();


    d3.select('svg g.chart')
        .selectAll('circle')
        .data(data)
        .enter().append('circle')
        .attr('fill', 'grey')
        .style('cursor', 'pointer')
        .attr('cx', function (d) {
            return xScale(d.x);
        })
        .attr('cy', function (d) {
            return yScale(d.y);
        })
        .attr('r', function (d) {
            return isNaN(d.x) || isNaN(d.y) ? 0 : 6;
        })
        .on('mouseover', function (d) {
            countryLabel
                .text(toString(d))
                .transition()
                .style('opacity', 1);
        })
        .on('mouseout', function (d) {
            
            countryLabel.text('');
            countryLabel.transition()
                .duration(1500)
                .style('opacity', 0);
        })
        .on('click', function (d) {
            
            window.open(d.info.href, '_blank');
        });


    d3.select('#xAxis').call(d3.axisBottom(xScale));
    d3.select('#yAxis').call(d3.axisLeft(yScale));

    // Update axis labels
    d3.select('#xLabel')
        .text('Area in M2');

    // Update correlation
    var xArray = _.map(data, function (d) { return d.x; });
    var yArray = _.map(data, function (d) { return d.y; });
    var c = getCorrelation(xArray, yArray);
    console.log(c);
    var x1 = xScale.domain()[0], y1 = c.m * x1 + c.b;
    var x2 = xScale.domain()[1], y2 = c.m * x2 + c.b;

    const bestfit = d3.select('#bestfit');
    // Fade in
    
    bestfit.attr( 'x1', xScale(x1));
    bestfit.attr('y1', yScale(y1));
    bestfit.attr('x2', xScale(x2));
    bestfit.attr('y2', yScale(y2));

}


function toString(dataPoint) {
    const flat = dataPoint.info;
    return flat.aream2 + ' m2 - ' + flat.price.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",") + ' EUR, ' + flat.district +  ' ' 
    + `rooms: ${flat.zimmerCount} -- ${flat.Title}`;
}



fetch('data.json').then(resp => {
    resp.json().then(/** @type {any[]} */ data => {



        const firstDistrict = data
        .filter(x => 
            parseFloat(x.district) < 1100 
            && parseFloat(x.price) < 500000 
            && parseFloat(x.price) > 10000  
            && x.category == "wohnung"
        )
        .map(flat => {
            return {x: parseFloat(flat.aream2), y: parseFloat(flat.price), info: flat};
        });

        updateChart(firstDistrict);


    });
});



/*
BauJahr: "1700"
IsNeubau: "0"
Lat: ""
Long: "0"
Title: "Uriges Wiener Beisl"
aream2: "115"
category: "gewerbe"
district: "1140"
href: "https://www.willhaben.at/iad/immobilien/d/gewerbeimmobilien-kaufen/wien/wien-1140-penzing/uriges-wiener-beisl-114172979/"
id: "114172979"
price: "269000"
rentorbuy: "buy"
street: ""
zimmerCount: "-1"
*/