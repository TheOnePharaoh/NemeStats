  DELETE FROM GamingGroup Where OwningUserId IN (SELECT Id FROM AspNetUsers WHERE email = 'jakejgordon3@gmail.com')
  DELETE FROM AspNetUsers WHERE email = 'jakejgordon3@gmail.com'