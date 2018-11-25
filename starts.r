
dat <- read.csv("C:/Users/../Desktop/wh.csv")
cheap <- dat[dat$price < 350000, ]
summary(cheap$district)
   Min. 1st Qu.  Median    Mean 3rd Qu.    Max. 
   1010    1030    1040    1045    1060    1090 
plot(cheap$price, cheap$aream2)
plot(cheap$aream2, cheap$price)
bezirk <- cheap[cheap$district == 1040, ]

plot(bezirk$aream2, bezirk$price)
abline(lm(bezirk$aream2 ~ bezirk$price))

plot(bezirk$aream2, bezirk$price)
abline(lm(bezirk$aream2 ~ bezirk$price))
plot(bezirk$aream2, bezirk$price)
abline(lm(bezirk$aream2 ~ bezirk$price))
View(bezirk)
View(cheap[cheap$price < 200000, ])
View(bezirk)
View(cheap[cheap$price < 200000, ])
options(scipen=999)

View(cheap[cheap$price < 200000, ])
View(cheap[cheap$price < 200000, ])
plot(cheap$aream2, cheap$price)