select *, (cheapRentPrice * aream2 * 12) / price as rendite from (
	select *, 
		price/aream2 as pm, 
		AvgForArea(flats.district, flats.aream2, 0.2, flats.rentorbuy, flats.category) as cheapBuyPrice, 
		AvgForArea(flats.district, flats.aream2, 0.2, 'rent', flats.category) as cheapRentPrice
	from flats 

	where deletedFromWh is null AND 
	price < 450000 AND
	-- district = 1040 &&
    rentOrBuy='buy' && price/aream2 < 5000 && (category='wohnung' or category='haus') 
    && title not like '%nbefristet%' && title not like '%Bungalow%' && title not like '%odernes Fertighaus%'  && title not like '%ertighaus%' 
    && price > 21000 
    
	having pm <= cheapBuyPrice+1 -- and aream2 >= plusRoomRequiredSize
	order by pm asc
) xx
having rendite > 0.01
order by rendite DESC