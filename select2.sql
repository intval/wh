
select *,
format(nettoRent * 12* 100/3, 0) as '3% Kp'

 from (

	select href, district, format(price, 0) as 'price', aream2, zimmerCount, street, addedOnWh, Title, format(pm,0) as 'pm', format(cheapBuyPrice, 0) as 'cheapBuyPrice', cheapRentPrice as 'cheap rent per m2', rentPriceForThatManyRooms as'cheap rentfor X rooms',
	round( (( LEAST( rentPriceForThatManyRooms, cheapRentPrice) -4) * aream2 * 12) / price , 3) as rendite,
    floor(aream2 * LEAST( rentPriceForThatManyRooms, cheapRentPrice) ) as 'bruttoRent',
	floor(4 * aream2) as 'bk',
	floor(aream2 * (LEAST( rentPriceForThatManyRooms, cheapRentPrice) -4)) as 'nettorent'

	from (
		select *, 
			round(price/aream2, 2) as pm, 
			floor(whscrapper.AvgForArea(flats.district, flats.aream2, 0.25, flats.rentorbuy, flats.category)) as cheapBuyPrice, 
			round(whscrapper.AvgForArea(flats.district, flats.aream2, 0.25, 'rent', flats.category), 2) as cheapRentPrice,
            round(whscrapper.AvgForRoomCount(flats.district, flats.zimmerCount, 0.25, flats.category), 2) as rentPriceForThatManyRooms
		from whscrapper.flats 

		where deletedFromWh is null AND 
		price < 550000 AND
		 -- district = 1200 AND
		 addedOnWh > date_sub(NOW(), interval 7 day) and
		rentOrBuy='buy' && price/aream2 < 6000 && (category='wohnung' or category='haus') 
		&& title not like '%nbefristet%' && title not like '%Bungalow%' && title not like '%odernes Fertighaus%'  && title not like '%ertighaus%' && title not like '%ersteigerung%' 
		&& price > 21000 
		
		having pm <= cheapBuyPrice-500 
		order by pm asc
	) xx
	having rendite > 0.01
	order by rendite DESC

) xy