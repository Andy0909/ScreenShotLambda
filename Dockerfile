# You can also pull these images from DockerHub amazon/aws-lambda-dotnet:6
FROM public.ecr.aws/lambda/dotnet:6

# Set the image's internal work directory
WORKDIR /var/task

# Copy function code
COPY "bin/Release/lambda-publish" .

# Set the CMD to your handler (could also be done as a parameter override outside of the Dockerfile)
CMD [ "ScreenShotLambda::ScreenShotLambda.Function::FunctionHandler" ]