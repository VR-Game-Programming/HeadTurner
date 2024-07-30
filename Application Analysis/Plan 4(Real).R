##################################################################################################
# Plan 4 (Using)
# Instead of analyzing the head rotation position and rotation angle of each turn, 
#  which was the goal of our previous plan, we will focus on analyzing the position 
#  where the head remains stationary in this plan. This will be presented by drawing 
## stacked bar charts showing different directions.

# Using and Reading Method of the Following Code:

# 1. Using
# Step 1: To use the code to draw stacked bar charts, you need to copy the code from 
#   the handle_outliers function to the Stacked_Bar_Plotter function, which spans from 
#   line 49 to 257 (inclusive), and paste it into the Console. Ensure that you have 
#   RStudio installed; if not, please refer to the readme.txt file. These are the 
#   functions needed to analyze the data and generate the graph.
# Step 2: Next, copy and paste the code in line 261 into the Console. This will invoke 
#   the categorization code. A prompt will appear in the Console asking you to enter the number 
#   of apps you wish to analyze. After entering this number, open the .csv files one by one,
#   make sure that the name of your files is the corresponding name of the apps.
# Step 3: Once all the files have been opened and the code execution is complete, copy and 
#   paste the code from line 263 into the Console. This code will invoke the plotting function. 
#   Type one of the three directions (Pitch, Yaw, Roll) to generate the final plot.
# Step 4: To obtain plots for all three directions, repeat the process by copying and pasting 
#   the code in line 266 after completing Steps 1 and 2. Enter "Pitch," "Yaw," and "Roll" 
#   each time to generate the respective plots.

# 2. Reading
# The following code performs two main tasks: categorizing the rotation angles and presenting 
#   them through stacked bar charts. It is divided into three main parts:
# PART 1 is responsible for categorization:
#   PART 1.1: This is the primary code for recording and analyzing data. It processes raw pitch, 
#     yaw, and roll data, categorizing them into different directional ranges. There are six 
#     ranges in degrees:
#     0 - 30 (inclusive)
#     30 - 60 (excluding 30, including 60)
#     60 - 90 (excluding 60, including 90)
#     90 - 120 (excluding 90, including 120)
#     120 - 150 (excluding 120, including 150)
#     150 - 180 (excluding 150, including 180)
#   PART 1.2: This code requires the user to open the .csv files and uses the functions from 
#     PART 1.1 to perform the categorization.
# PART 2: This is the plotting code. You should have ggplot2 installed to successfully generate 
#     the plots.

##################################################################################################
## PART 1.1: CATEGORIZATION FUNCTIONS
##################################################################################################

## handle_outliers deals with the data that's greater than 180 or smaller than -180,
##  and returns the list contains pitch_vec, yaw_vec and roll_vec in this order.
handle_outliers <- function(pitch_vec, yaw_vec, roll_vec, valid_row, valid_length) {
  
  for (i in valid_row:valid_length) {
    ## pitch
    if (pitch_vec[i] > 180) {
      pitch_vec[i] <- pitch_vec[i] - 360
    } 
    else if (pitch_vec[i] < -180) {
      pitch_vec[i] <- pitch_vec[i] + 360
    }
    ## yaw
    if (yaw_vec[i] > 180) {
      yaw_vec[i] <- yaw_vec[i] - 360
    } 
    else if (yaw_vec[i] < -180) {
      yaw_vec[i] <- yaw_vec[i] + 360
    }
    ## roll
    if (roll_vec[i] > 180) {
      roll_vec[i] <- roll_vec[i] - 360
    } 
    else if (roll_vec[i] < -180) {
      roll_vec[i] <- roll_vec[i] + 360
    }
  } 
  data_list <- list(pitch_vec, yaw_vec, roll_vec)
  return(data_list)
}

## determine_range is a helper function of position_counter, it categorizes 
##  the angles in different ranges, as the introduction in the beginning, 
##  we have a total of six different ranges.
determine_range <- function(position_vec, counting_list) {
  for (i in 1:length(position_vec)) {
    if (0 <= abs(position_vec[i]) && abs(position_vec[i]) <= 30) {
      counting_list[[1]] <- counting_list[[1]] + 1
    } else if (30 < abs(position_vec[i]) && abs(position_vec[i]) <= 60) {
      counting_list[[2]] <- counting_list[[2]] + 1
    } else if (60 < abs(position_vec[i]) && abs(position_vec[i]) <= 90) {
      counting_list[[3]] <- counting_list[[3]] + 1
    } else if (90 < abs(position_vec[i]) && abs(position_vec[i]) <= 120) {
      counting_list[[4]] <- counting_list[[4]] + 1
    } else if (120 < abs(position_vec[i]) && abs(position_vec[i]) <= 150) {
      counting_list[[5]] <- counting_list[[5]] + 1
    } else if (150 < abs(position_vec[i]) && abs(position_vec[i]) <= 180) {
      counting_list[[6]] <- counting_list[[6]] + 1
    } else {
      stop("determine_range, angle range > 180 degree.")
    }
  }
  return(counting_list)
}

## This function categorizes the positions pitch, yaw and roll to one
##  of the six ranges in degrees and put all the results into a list.
position_counter <- function(position_list, valid_row, valid_length) {
  ## handle_outliers <- function(pitch_vec, yaw_vec, roll_vec, valid_row, valid_length)
  ## Index : Range
  ## 1: 0 - 30
  ## 2: 30 - 60
  ## 3: 60 - 90
  ## 4: 90 - 120
  ## 5: 120 - 150
  ## 6: 150 - 180
  position_list <- handle_outliers(position_list[[1]], position_list[[2]], position_list[[3]],
                                   valid_row, valid_length)
  pitch_count <- list(0,0,0,0,0,0)
  yaw_count <- list(0,0,0,0,0,0)
  roll_count <- list(0,0,0,0,0,0)
  
  for (i in 1:length(position_list)) {
    
    if (i == 1) {
      pitch_count <- determine_range(position_list[[i]], pitch_count)
    }
    else if (i == 2) {
      yaw_count <- determine_range(position_list[[i]], yaw_count)
    }
    else if (i == 3) {
      roll_count <- determine_range(position_list[[i]], roll_count)
    }
    else {
      stop("position_counter, length out of range 3.")
    }
  }
  pack_list <- list(pitch_count, yaw_count, roll_count)
  return(pack_list)
}

##################################################################################################
## PART 1.2: CATEGORIZATION IMPLEMENTATION
##################################################################################################

Analysis_Func <- function() {
  trying_limit <- 3
  trying_times <- 0
  while (TRUE) {
    input_val <- readline(prompt = "Please enter the number of apps that need to be analyzed (Integer > 0 Only): ")
    if (is.integer(input_val)) {
      break
    }
    trying_times <- trying_times + 1
    if (trying_times == trying_limit) {
      stop("Invalid Number. Too many tries.")
    }
  }
  app_num <- as.numeric(input_val)
  
  data <- data.frame(
    App = character(),
    Range = character(),
    Proportion = numeric(),
    stringsAsFactors = FALSE
  )
  apps_list <- list()
  
  for (i in 1:app_num) {
    f <- file.choose()
    app_data <- read.csv(f)
    app_name <- tools::file_path_sans_ext(basename(f))
    
    pitch_data <- app_data$Pitch[1:length(app_data$Pitch)]
    yaw_data <- app_data$Yaw[1:length(app_data$Yaw)]
    roll_data <- app_data$Roll[1:length(app_data$Roll)]
    app_time <- app_data$time[1:length(app_data$time)]
    valid_data_length <- length(app_time)
    
    app_pack_list <- position_counter(list(pitch_data, yaw_data, roll_data), 1, valid_data_length)
    app_pack_list <- append(app_pack_list, app_name)
    apps_list <- append(apps_list, list(app_pack_list))
  }
  return(apps_list)
}

##################################################################################################
## PART 2: PLOTTING
##################################################################################################

Stacked_Bar_Plotter <- function(apps_list) {
  direction <- readline(prompt = "Please enter the direction in one word (One of Pitch, Yaw or Roll) eg. Pitch: ")
  
  library(ggplot2)
  # Create an empty data frame to store the counts
  data <- data.frame(
    App = character(),
    Range = character(),
    Proportion = numeric(),
    stringsAsFactors = FALSE
  )
  
  trying_limit <- 3
  trying_time <- 0
  while (TRUE) {
    if (direction == "Pitch") {
      dir_idx <- 1
    }
    else if (direction == "Yaw") {
      dir_idx <- 2
    }
    else if (direction == "Roll") {
      dir_idx <- 3
    }
    trying_time <- trying_time + 1
    if (trying_time == trying_limit) {
      stop("Wrong direction name. Too many tries.")
    }
    else {
      print("Wrong direction name, please remind the capital letters and spellings.")
    }
  }
  
  # Iterate through each app, calculating the proportion of each action
  for (i in 1:length(apps_list)) {
    app_data <- apps_list[[i]][[dir_idx]] 
    
    total_angles <- sum(unlist(app_data))
    
    if (total_angles == 0) {
      total_angles <- 1 
    }
    
    # Add proportion data to the data frame
    data <- rbind(data, data.frame(App = apps_list[[i]][[4]], Range = "0-30", Proportion = app_data[[1]] / total_angles))
    data <- rbind(data, data.frame(App = apps_list[[i]][[4]], Range = "30-60", Proportion = app_data[[2]] / total_angles))
    data <- rbind(data, data.frame(App = apps_list[[i]][[4]], Range = "60-90", Proportion = app_data[[3]] / total_angles))
    data <- rbind(data, data.frame(App = apps_list[[i]][[4]], Range = "90-120", Proportion = app_data[[4]] / total_angles))
    data <- rbind(data, data.frame(App = apps_list[[i]][[4]], Range = "120-150", Proportion = app_data[[5]] / total_angles))
    data <- rbind(data, data.frame(App = apps_list[[i]][[4]], Range = "150-180", Proportion = app_data[[6]] / total_angles))
  }
  
  data$Range <- factor(data$Range, levels = c("0-30", "30-60", "60-90", "90-120", "120-150", "150-180"))
  
  ggplot(data, aes(x = App, y = Proportion, fill = Range)) +
    geom_bar(stat = "identity") +
    scale_fill_manual(name = "Range in Degrees", 
                      values = c("0-30" = "lightskyblue", 
                                 "30-60" = "deepskyblue", 
                                 "60-90" = "dodgerblue", 
                                 "90-120" = "blue", 
                                 "120-150" = "mediumblue", 
                                 "150-180" = "darkblue")) + 
    labs(title = paste("Proportion of Head Movement in Different Angle Ranges \nin the", 
                       direction, 
                       "Direction of Various Apps"),
         x = "App",
         y = "Proportion") +
    theme_minimal()
}

##################################################################################################
## Envoking Functions Above
## 1 Time
apps_list <- Analysis_Func()

## 3 Times(1 time for each direction, Pitch, Yaw or Roll)
Stacked_Bar_Plotter(apps_list)










