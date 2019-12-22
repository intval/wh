import mysql = require('mysql2')

var connection = mysql.createConnection({
  host     : 'localhost',
  user     : 'root',
  password : '123456',
  database : 'whscrapper'
});

const action = 'rent';

connection.connect(function(err) {
  
    let table = {};
    let allPromises = [];

    for(let d = 1; d <= 5; d++)
    {
        const district = `1${d.toString().padStart(2, '0')}0`;  
        table[`x` + district]   = {};
        for(let area = 20; area <= 200; area += 10)
        {
            allPromises.push(new Promise((resolve, reject) => {

                const cmd = pm(action, district, area, (area == 200) ? 5000 : area+9);
                connection.query(cmd, function(err, results, fields){
                    if(err) {
                        console.log(err); 
                        reject(err);
                    }
                    // console.log(results);
                    if(results.length) {
                        table[`x` + district][`${area}-${area == 200 ? '': area+9}`] = parseFloat(results[0].pm).toFixed(2);
                    } else {
                        console.log('no results found for', district, area)
                    }
                    resolve();
                });
                
            }));
            
        }
    }

    Promise.all(allPromises).then(() => {
        for(let k in table) {
            for(let area in table[k]) {
                const updQuery = `update prices set ${action}PerM2 = ${table[k][area]} where district=${k.substr(1, 4)} and areaM2 = '${area}'`;
                console.log(updQuery);
            }
        }
    }).then(() => {
        connection.destroy();
        // console.log(table);
        console.log(JSON.stringify(table));
    });
    
});


function pm(rentOrBuy, district, areaFrom , areaTo) {
    return `
		select * from  (
				select 
					district,
					price/aream2 as pm, -- aream2,
					cume_dist() over (order by price/aream2) dist
					from flats
				where category='wohnung' and rentOrBuy = '${rentOrBuy}'
                and district = ${district}
                and aream2 >= ${areaFrom} and aream2 <= ${areaTo}
                order by dist asc
				-- and zimmerCount = 2 
				
		) x
		 group by x.pm
		 having dist >= 0.1 and dist < 0.2
		 limit 1
	 `;
}