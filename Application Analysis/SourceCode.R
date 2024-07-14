## PLAN OF IMPLEMENTATION FOR ANALYSIS AND PLOTTING OF OPTITRACK USER DATA
## Description:
##  The following code is divided into two parts:

##  The first part is the analysis part, which aims to 
##  do one major thing: analyze the data collected 
##  by OptiTrack in .csv files. It analyzes and determines, 
##  with the passage of time, whether or not the data shows the 
##  user turning his/her head, and in which position (pitch, yaw, roll, etc.).

##  The second part is the plotting part, where we mainly use the ggplot2 
##  library to draw all the graphs.


##################################################################################################
## PART 1: ANALYSIS METHODS
##################################################################################################

## PLAN 1 (abandoned, so not complete)

## Method: Compare two adjacent data points and calculate 
##  their difference. If the difference is larger than our
##  self-defined threshold, then we count it as a turn
##  of the head.
## Reason for abandonment: It's very difficult to find
##  a consistent threshold to differentiate between
##  valid movements and invalid ones. Small changes 
##  can make a big difference.
vdl_temp <- bs_valid_data_length
bs_valid_data_length <- 0
i <- 1
bs_data_count <- c(0, 0, 0, 0, 0 ,0 ,0)
differ <- 0.1

## 1 for pitch; 2 for yaw; 3 for roll; 
## 4 for pitch + yaw; 5 for pitch + roll;
## 6 for yaw + roll; 7 for pitch + yaw + roll
valid_length <- bs_valid_data_length
bs_pitch_data <- vector("numeric", length = valid_length)
bs_yaw_data <- vector("numeric", length = valid_length)
bs_roll_data <- vector("numeric", length = valid_length)

j <- 1
current_time <- bs_time[1]
record_idx <- 1

while (i < vdl_temp) {
  if (abs(current_time - bs_time[i]) >= 0.1) {
    current_time <- bs_time[i]
    if (abs(bs_pitch_cpy[i] - bs_pitch_cpy[record_idx]) > differ |
        abs(bs_yaw_cpy[i] - bs_yaw_cpy[record_idx]) > differ |
        abs(bs_roll_cpy[i] - bs_roll_cpy[record_idx]) > differ) {
      if (abs(bs_pitch_cpy[i] - bs_pitch_cpy[record_idx]) > differ &&
          abs(bs_yaw_cpy[i] - bs_yaw_cpy[record_idx]) > differ &&
          abs(bs_roll_cpy[i] - bs_roll_cpy[record_idx]) > differ) {
        bs_data_count[7] <- bs_data_count[7] + 1
      } 
      else if (abs(bs_pitch_cpy[i] - bs_pitch_cpy[record_idx]) > differ &&
              abs(bs_yaw_cpy[i] - bs_yaw_cpy[record_idx]) > differ) {
        bs_data_count[4] <- bs_data_count[4] + 1
      } 
      else if (abs(bs_pitch_cpy[i] - bs_pitch_cpy[record_idx]) > differ &&
              abs(bs_roll_cpy[i] - bs_roll_cpy[record_idx]) > differ) {
        bs_data_count[5] <- bs_data_count[5] + 1
      } 
      else if (abs(bs_yaw_cpy[i] - bs_yaw_cpy[record_idx]) > differ &&
              abs(bs_roll_cpy[i] - bs_roll_cpy[record_idx]) > differ) {
        bs_data_count[6] <- bs_data_count[6] + 1
      } 
      else if (abs(bs_pitch_cpy[i] - bs_pitch_cpy[record_idx]) > differ) {
        bs_data_count[1] <- bs_data_count[1] + 1
      } 
      else if (abs(bs_yaw_cpy[i] - bs_yaw_cpy[record_idx]) > differ) {
        bs_data_count[2] <- bs_data_count[2] + 1
      } 
      else {
        bs_data_count[3] <- bs_data_count[3] + 1
      }
      bs_valid_data_length <- bs_valid_data_length + 1
      bs_pitch_data[j] <- bs_pitch_cpy[j]
      bs_yaw_data[j] <- bs_yaw_cpy[j]
      bs_roll_data[j] <- bs_roll_cpy[j]
      j <- j + 1
      record_idx <- i
    }
  }
    i <- i + 1
}
##################################################################################################

## PLAN 2 (Using)
## Method description: Data Processing: The original dataset contains a large number of 
##  data points with angles exceeding 180 degrees, with many even approaching 
##  360 degrees. It is my judgment that these data points are likely measurements 
##  from the negative coordinate axis by OptiTrack. This is because, under normal 
##  circumstances, the range for pitch, yaw, and roll should be between -180 and 180 
##  degrees. Therefore, the first step is to process the data such that any value 
##  exceeding 180 degrees is reduced by 360 degrees to obtain the correct rotational 
##  coordinate angle.
## The most crucial aspect is determining what constitutes a head turn. 
##  I have adopted the threshold defined by OptiTrack Prime^x 13, which 
##  indicates that each measurement may have a maximum error of 0.5 degrees. 
##  If a segment of data changes its trend (i.e., the sign of the difference 
##  between consecutive data points changes), we compare this segment to the 
##  threshold. If the change exceeds the threshold, it is considered a valid 
##  head turn. Therefore, the starting point of a head turn is the first value, 
##  and the ending point is when the sign of the value changes.
## This description pertains to determining rotation in a single direction. 
##  When considering combinations of rotations in the pitch, yaw, and roll directions, 
##  we need to determine the minimum change across the three directions and identify 
##  which directions' movements are valid within this smallest segment. There are eight 
##  possible combinations:
## 1. Pure pitch
## 2. Pure roll
## 3. Pure yaw
## 4. Pitch + roll
## 5. Pitch + yaw
## 6. Roll + yaw
## 7. Pitch + yaw + roll
## 8. No motion
## These eight combinations are recorded in the data_count vector. 
##  Whenever a movement is determined to fall under any of these categories, 
##  the angle change is recorded into the corresponding filtered vector.

## Implementation:

## count_same_trend counts the number of increments in a section determines 
##  how many data points will be counted and returns the length.
count_same_trend <- function(data_vec, idx, valid_length) {
  same_trend_num <- 0
  trend <- ifelse(data_vec[idx + 1] - data_vec[idx] >= 0, 1, -1)
  
  while (idx < valid_length) {
    
    if ((data_vec[idx + 1] - data_vec[idx]) * trend < 0) {
      return(same_trend_num)
    } else {
      same_trend_num <- same_trend_num + 1
      idx <- idx + 1
    }
  }
  return(same_trend_num)
}

## Check whether it is a valid head turn and return a list that includes 
##  a boolean and the rotation angle.
check_valid_rotation <- function(data_vec, idx, threshold, valid_length) {
  data_sum <- 0
  trend <- ifelse(data_vec[idx] >= 0, 1, -1)
  rotation_data <- list(F, 0)
  while (idx < valid_length) {
    ## Debug Usage
    ##if (abs(data_vec[idx + 1] - data_vec[idx] > 180)) {
    ##  print(paste("last idx:", idx + 1))
    ##  print(paste("last val:", data_vec[idx + 1]))
    ##  print(paste("next idx:", idx))
    ##  print(paste("next val:", data_vec[idx]))
    ##}
    if (((data_vec[idx + 1] - data_vec[idx]) * trend) < 0) {
      break
    } else {
      data_sum <- data_sum + abs(data_vec[idx + 1] - data_vec[idx])
      idx <- idx + 1
    }
  }
  ## Debug Usage
  ##if (data_sum > threshold) {
  ##  rotation_data[[1]] <- T
  ##  rotation_data[[2]] <- data_sum
  ##  if (data_sum > 180) {
  ##    print("Error")
  ##  }
  ##}
  return(rotation_data)
}


## handle_outliers deals with the data that's greater than 180 or smaller than -180,
##  and returns the list contains pitch_vec, yaw_vec and roll_vec in this order.
handle_outliers <- function(pitch_vec, yaw_vec, roll_vec, valid_row, valid_length) {
  
  for (i in valid_row:valid_length) {
    if (pitch_vec[i] > 180) {
      pitch_vec[i] <- pitch_vec[i] - 360
    } else if (pitch_vec[i] < -180) {
      pitch_vec[i] <- pitch_vec[i] + 360
    }
    if (yaw_vec[i] > 180) {
      yaw_vec[i] <- yaw_vec[i] - 360
    } else if (yaw_vec[i] < -180) {
      yaw_vec[i] <- yaw_vec[i] + 360
    }
    if (roll_vec[i] > 180) {
      roll_vec[i] <- roll_vec[i] - 360
    } else if (roll_vec[i] < -180) {
      roll_vec[i] <- roll_vec[i] + 360
    }
  } 
  cpy_list <- list(pitch_vec, yaw_vec, roll_vec)
  return(cpy_list)
}

## data_filter_counter Function: This function will count the combinations 
##  of rotations in the pitch, yaw, and roll directions, compiling them into 
##  the data_count vector. It will also record the angle changes of each valid 
##  head turn into the corresponding filtered vector. The function ultimately 
##  returns a list containing the four pieces of information described below:

##  index 1 contains a vector that includes the counting results
##  of different head rotation combinations;
##  index 2 contains the vector of filtered pitch data;
##  index 3 contains the vector of filtered yaw data;
##  index 4 contains the vector of filtered roll data;
data_filter_counter <- function(pitch_data_vec, yaw_data_vec, roll_data_vec, 
                                current_idx, valid_length, 
                                valid_threshold) {
  data_count <- c(0, 0, 0, 0, 0 ,0 ,0, 0)
  pitch_filtered_data <- numeric()
  yaw_filtered_data <- numeric()
  roll_filtered_data <- numeric()
  
  ## Handle the absolute angles > 180 degrees to the corrected range
  
  vec_list <- handle_outliers(pitch_data_vec, yaw_data_vec, roll_data_vec, 
                              current_idx, valid_length)
  pitch_data_vec <- vec_list[[1]]
  yaw_data_vec <- vec_list[[2]]
  roll_data_vec <- vec_list[[3]]
  
  ## Debug Usage
  ##for (i in current_idx:valid_length) {
  ##  if (abs(pitch_data_vec[i]) > 180) {
  ##    print(paste("pitch idx:", i, "value:", pitch_data_vec[i]))
  ##  } 
  ##  if (abs(yaw_data_vec[i]) > 180) {
  ##    print(paste("yaw idx:", i, "value:", yaw_data_vec[i]))
  ##  }
  ##  if (abs(roll_data_vec[i]) > 180) {
  ##    print(paste("roll idx:", i, "value:", roll_data_vec[i]))
  ##  }
  ##}
  
  ## Debug Usage
  ##if (any(abs(pitch_data_vec) > 180) || 
  ##    any(abs(yaw_data_vec) > 180) || 
  ##    any(abs(roll_data_vec) > 180)) {
  ## stop("Data still contains values exceeding 180 degrees after handling outliers. Front")
  ##}
  
  while (current_idx < valid_length) {
    
    ## Number of elements in the same trend
    pitch_trend_num <- count_same_trend(pitch_data_vec, current_idx, valid_length)
    yaw_trend_num <- count_same_trend(yaw_data_vec, current_idx, valid_length)
    roll_trend_num <- count_same_trend(roll_data_vec, current_idx, valid_length)
    min_trend_num <- min(pitch_trend_num, yaw_trend_num, roll_trend_num)
    interval <- min_trend_num + current_idx
    pitch_turned <- check_valid_rotation(pitch_data_vec, 
                                         current_idx, 
                                         valid_threshold, 
                                         interval)
    yaw_turned <- check_valid_rotation(yaw_data_vec, 
                                       current_idx, 
                                       valid_threshold, 
                                       interval)
    roll_turned <- check_valid_rotation(roll_data_vec, 
                                        current_idx, 
                                        valid_threshold, 
                                        interval)
    ## 1 for pitch; 2 for yaw; 3 for roll; 
    ## 4 for pitch + yaw; 5 for pitch + roll;
    ## 6 for yaw + roll; 7 for pitch + yaw + roll
    ## 8 for no motion
    if (pitch_turned[[1]] | yaw_turned[[1]] | roll_turned[[1]]) {
      if (pitch_turned[[1]] && yaw_turned[[1]] && roll_turned[[1]]) {
        data_count[7] <- data_count[7] + 1
        pitch_filtered_data <- c(pitch_filtered_data, pitch_turned[[2]])
        yaw_filtered_data <- c(yaw_filtered_data, yaw_turned[[2]])
        roll_filtered_data <- c(roll_filtered_data, roll_turned[[2]])
      }
      else if (pitch_turned[[1]] && yaw_turned[[1]]) {
        data_count[4] <- data_count[4] + 1
        pitch_filtered_data <- c(pitch_filtered_data, pitch_turned[[2]])
        yaw_filtered_data <- c(yaw_filtered_data, yaw_turned[[2]])
      }
      else if (pitch_turned[[1]] && roll_turned[[1]]) {
        data_count[5] <- data_count[5] + 1
        pitch_filtered_data <- c(pitch_filtered_data, pitch_turned[[2]])
        roll_filtered_data <- c(roll_filtered_data, roll_turned[[2]])
      }
      else if (yaw_turned[[1]] && roll_turned[[1]]) {
        data_count[6] <- data_count[6] + 1
        yaw_filtered_data <- c(yaw_filtered_data, yaw_turned[[2]])
        roll_filtered_data <- c(roll_filtered_data, roll_turned[[2]])
      }
      else if (pitch_turned[[1]]) {
        data_count[1] <- data_count[1] + 1
        pitch_filtered_data <- c(pitch_filtered_data, pitch_turned[[2]])
      }
      else if (yaw_turned[[1]]) {
        data_count[2] <- data_count[2] + 1
        yaw_filtered_data <- c(yaw_filtered_data, yaw_turned[[2]])
      }
      else {
        data_count[3] <- data_count[3] + 1
        roll_filtered_data <- c(roll_filtered_data, roll_turned[[2]])
      }
    }
    else {
      data_count[8] <- data_count[8] + 1
    }
    current_idx <- current_idx + min_trend_num
  }
  ## Debug Usage
  ##if (any(abs(pitch_filtered_data) > 180) || 
  ##    any(abs(yaw_filtered_data) > 180) || 
  ##    any(abs(roll_filtered_data) > 180)) {
  ##  stop("Data still contains values exceeding 180 degrees after handling outliers. Back")
  ##}
  pack_list <- list(data_count, pitch_filtered_data, yaw_filtered_data, roll_filtered_data)
  return(pack_list)
}


##################################################################################################
## PART 2: INSTANCE ANALYSIS
##################################################################################################

## APPs Names
##################################
## 1.Beat Saber

## Initialize data
## Open our file (it should strictly align with our code, 
##  as some of the data is unique to this specific file).
f1 <- file.choose()
bs_data <- read.csv(f1)
valid_threshold <- 0.5

valid_row <- 2
bs_pitch_data <- bs_data$Pitch[valid_row:length(bs_data$Pitch)]
bs_yaw_data <- bs_data$Yaw[valid_row:length(bs_data$Yaw)]
bs_roll_data <- bs_data$Roll[valid_row:length(bs_data$Roll)]
bs_time <- bs_data$time[valid_row:length(bs_data$time)]
bs_valid_data_length <- length(bs_time)


## We make a copy of our data to show which data 
##  has been deleted and how much.
bs_pitch_cpy <- bs_pitch_data
bs_yaw_cpy <- bs_yaw_data
bs_roll_cpy <- bs_roll_data


## Incetance
valid_row <- 1
bs_pack_list <- data_filter_counter(bs_pitch_cpy, 
                                    bs_yaw_cpy, 
                                    bs_roll_cpy, 
                                    valid_row, 
                                    bs_valid_data_length, 
                                    valid_threshold)
##################################################################################################
## 2.First Hand
f2 <- file.choose()
fh_data <- read.csv(f2)
valid_threshold <- 0.5
valid_row <- 2

fh_pitch_data <- fh_data$Pitch[valid_row:length(fh_data$Pitch)]
fh_yaw_data <- fh_data$Yaw[valid_row:length(fh_data$Yaw)]
fh_roll_data <- fh_data$Roll[valid_row:length(fh_data$Roll)]
fh_time <- fh_data$time[valid_row:length(fh_data$time)]
fh_valid_data_length <- length(fh_time)


## We make a copy of our data to show which data 
##  has been deleted and how much.
fh_pitch_cpy <- fh_pitch_data
fh_yaw_cpy <- fh_yaw_data
fh_roll_cpy <- fh_roll_data

# Instance
valid_row <- 1
fh_pack_list <- data_filter_counter(fh_pitch_cpy, 
                                    fh_yaw_cpy, 
                                    fh_roll_cpy, 
                                    valid_row, 
                                    fh_valid_data_length, 
                                    valid_threshold)
##################################################################################################
## 3.Super Hot
f3 <- file.choose()
sh_data <- read.csv(f3)
valid_threshold <- 0.5
valid_row <- 2

sh_pitch_data <- sh_data$Pitch[valid_row:length(sh_data$Pitch)]
sh_yaw_data <- sh_data$Yaw[valid_row:length(sh_data$Yaw)]
sh_roll_data <- sh_data$Roll[valid_row:length(sh_data$Roll)]
sh_time <- sh_data$time[valid_row:length(sh_data$time)]
sh_valid_data_length <- length(sh_time)


## We make a copy of our data to show which data 
##  has been deleted and how much.
sh_pitch_cpy <- sh_pitch_data
sh_yaw_cpy <- sh_yaw_data
sh_roll_cpy <- sh_roll_data


# Instance
valid_row <- 1
sh_pack_list <- data_filter_counter(sh_pitch_cpy, 
                                    sh_yaw_cpy, 
                                    sh_roll_cpy, 
                                    valid_row, 
                                    sh_valid_data_length, 
                                    valid_threshold)


##################################################################################################
## 4.EcoSphere dataset 1
## Comment: the reason of seperate EcoSphere into two parts is,
##  I tried to watch two different videos in this app so I want
##  to analysis them seperately.
f4 <- file.choose()
es1_data <- read.csv(f4)
valid_threshold <- 0.5
valid_row <- 2


es1_pitch_data <- es1_data$Pitch[valid_row:length(es1_data$Pitch)]
es1_yaw_data <- es1_data$Yaw[valid_row:length(es1_data$Yaw)]
es1_roll_data <- es1_data$Roll[valid_row:length(es1_data$Roll)]
es1_time <- es1_data$time[valid_row:length(es1_data$time)]
es1_valid_data_length <- length(es1_time)


## We make a copy of our data to show which data 
##  has been deleted and how much.
es1_pitch_cpy <- es1_pitch_data
es1_yaw_cpy <- es1_yaw_data
es1_roll_cpy <- es1_roll_data



# Instance
valid_row <- 1
es1_pack_list <- data_filter_counter(es1_pitch_cpy, 
                                    es1_yaw_cpy, 
                                    es1_roll_cpy, 
                                    valid_row, 
                                    es1_valid_data_length, 
                                    valid_threshold)

##################################################################################################
## 5.EcoSphere dataset 2
f5 <- file.choose()
es2_data <- read.csv(f5)
valid_threshold <- 0.5
valid_row <- 2


es2_pitch_data <- es2_data$Pitch[valid_row:length(es2_data$Pitch)]
es2_yaw_data <- es2_data$Yaw[valid_row:length(es2_data$Yaw)]
es2_roll_data <- es2_data$Roll[valid_row:length(es2_data$Roll)]
es2_time <- es2_data$time[valid_row:length(es2_data$time)]
es2_valid_data_length <- length(es2_time)


## We make a copy of our data to show which data 
##  has been deleted and how much.
es2_pitch_cpy <- es2_pitch_data
es2_yaw_cpy <- es2_yaw_data
es2_roll_cpy <- es2_roll_data


# Instance
valid_row <- 1
es2_pack_list <- data_filter_counter(es2_pitch_cpy, 
                                    es2_yaw_cpy, 
                                    es2_roll_cpy, 
                                    valid_row, 
                                    es2_valid_data_length, 
                                    valid_threshold)
##################################################################################################
## Test
d1_test <- c(2,3,4,1,2,9)
d2_test <- c(-2,-3,-5,-7,-9,1)
d3_test <- c(-2,2,-2,2,-2,2)
valid_length_test <- 6
valid_row_test <- 1
valid_thres_test <- 2

test_pack_list <- data_filter_counter(d1_test, d2_test, d3_test, valid_row_test,
                                      valid_length_test, valid_thres_test)

##################################################################################################
## PART 3: PLOTTING
##################################################################################################

## TIME-BASED
# Stacked bar chart
# Assume apps is already defined
apps <- list(bs_pack_list, fh_pack_list, sh_pack_list, es1_pack_list, es2_pack_list)

# Create an empty data frame to store proportion data
data <- data.frame(
  App = character(),
  Action = character(),
  Proportion = numeric(),
  stringsAsFactors = FALSE
)

# Define the names of each app
app_names <- c("Beat Saber", "First Hand", "Super Hot", "EcoSphere1", "EcoSphere2")

# Iterate through each app, calculating the proportion of each action
for (i in 1:length(apps)) {
  app_data <- apps[[i]][[1]]
  total_actions <- sum(app_data)
  
  
  # Add proportion data to the data frame
  data <- rbind(data, data.frame(App = app_names[i], Action = "None", Proportion = app_data[8] / total_actions))
  data <- rbind(data, data.frame(App = app_names[i], Action = "Pitch", Proportion = app_data[1] / total_actions))
  data <- rbind(data, data.frame(App = app_names[i], Action = "Yaw", Proportion = app_data[2] / total_actions))
  data <- rbind(data, data.frame(App = app_names[i], Action = "Roll", Proportion = app_data[3] / total_actions))
  data <- rbind(data, data.frame(App = app_names[i], Action = "Pitch+Yaw", Proportion = app_data[4] / total_actions))
  data <- rbind(data, data.frame(App = app_names[i], Action = "Pitch+Roll", Proportion = app_data[5] / total_actions))
  data <- rbind(data, data.frame(App = app_names[i], Action = "Yaw+Roll", Proportion = app_data[6] / total_actions))
  data <- rbind(data, data.frame(App = app_names[i], Action = "Pitch+Yaw+Roll", Proportion = app_data[7] / total_actions))
}

# Use ggplot2 to plot the stacked bar chart
library(ggplot2)

ggplot(data, aes(x = App, y = Proportion, fill = Action)) +
  geom_bar(stat = "identity") +
  scale_fill_manual(values = c("None" = "gray", 
                               "Pitch" = "blue", 
                               "Yaw" = "green", 
                               "Roll" = "red", 
                               "Pitch+Yaw" = "purple", 
                               "Pitch+Roll" = "orange", 
                               "Yaw+Roll" = "pink", 
                               "Pitch+Yaw+Roll" = "yellow")) + 
  labs(title = "Proportion of Head Movements in Different Apps",
       x = "App",
       y = "Proportion") +
  theme_minimal()

## Angle-based

# 1.Box Plot

boxplot(list( bs_pack_list[[2]], fh_pack_list[[2]], sh_pack_list[[2]], 
              es1_pack_list[[2]], es2_pack_list[[2]]),
        names = c("Beat Saber", "First Hand", "Super Hot", "EcoSphere1", "EcoSphere2"),
        main = "Comparison of Pitch Data across Applications",
        outline = FALSE,
        xlab = "Degree")

boxplot(list( bs_pack_list[[3]], fh_pack_list[[3]], sh_pack_list[[3]], 
              es1_pack_list[[3]], es2_pack_list[[3]]),
        names = c("Beat Saber", "First Hand", "Super Hot", "EcoSphere1", "EcoSphere2"),
        main = "Comparison of Yaw Data across Applications",
        outline = FALSE,
        xlab = "Degree")

boxplot(list( bs_pack_list[[4]], fh_pack_list[[4]], sh_pack_list[[4]], 
              es1_pack_list[[4]], es2_pack_list[[4]]),
        names = c("Beat Saber", "First Hand", "Super Hot", "EcoSphere1", "EcoSphere2"),
        main = "Comparison of Roll Data across Applications",
        outline = FALSE,
        xlab = "Degree")


## 2.Bar Charts

# Put all the data into the list
data_list <- list(
  Beat_Saber = list(Pitch = bs_pack_list[[2]], Yaw = bs_pack_list[[3]], Roll = bs_pack_list[[4]]),
  First_Hand = list(Pitch = fh_pack_list[[2]], Yaw = fh_pack_list[[3]], Roll = fh_pack_list[[4]]), 
  Super_Hot = list(Pitch = sh_pack_list[[2]], Yaw = sh_pack_list[[3]], Roll= sh_pack_list[[4]]), 
  EcoSphere1 = list(Pitch = es1_pack_list[[2]], Yaw = es1_pack_list[[3]], Roll = es1_pack_list[[4]]), 
  EcoSphere2 = list(Pitch = es2_pack_list[[2]], Yaw = es2_pack_list[[3]], Roll = es2_pack_list[[4]])
)

# Get the MAX result
results <- data.frame(
  Application = character(),
  Direction = character(),
  Degree = numeric()
)

for (app in names(data_list)) {
  for (dir in names(data_list[[app]])) {
    max_value <- max(abs(data_list[[app]][[dir]]))
    results <- rbind(results, data.frame(Application = app, Direction = dir, Degree = max_value))
  }
}

ggplot(results, aes(x = Application, y = Degree, fill = Direction)) +
  geom_bar(stat = "identity", position = "dodge") +
  scale_fill_manual(values = c("Pitch" = "red", "Yaw" = "blue", "Roll" = "green")) +
  theme_minimal() +
  labs(title = "Max Values of Pitch, Yaw, and Roll for Each Application",
       x = "Application",
       y = "Degree",
       fill = "Direction")

## Debug Usage
##for (i in 1:length(fh_pack_list[[4]])) {
##  if (fh_pack_list[[4]][i] >= 200) {
##    print(i)
##  }
##}

## Debug Usage
##i <- 4800
##while (i < length(fh_pack_list[[4]])) {
##  print(fh_pack_list[[4]][i])
##  i <- i + 1
##}

